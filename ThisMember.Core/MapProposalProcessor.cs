﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using ThisMember.Core.Interfaces;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace ThisMember.Core
{
  internal class ExpressionTuple
  {
    public Expression OldExpression { get; set; }
    public Expression NewExpression { get; set; }

    public ExpressionTuple(Expression oldExpr, Expression newExpr)
    {
      OldExpression = oldExpr;
      NewExpression = newExpr;
    }
  }


  internal class MapExpressionProcessor
  {
    public ICollection<ExpressionTuple> ParametersToReplace { get; private set; }

    public bool NonPublicMembersAccessed { get; private set; }

    public MapExpressionProcessor(IMemberMapper mapper)
    {
      ParametersToReplace = new HashSet<ExpressionTuple>();
      this.MemberMapper = mapper;
    }

    public Expression RootExpression { get; private set; }

    public IMemberMapper MemberMapper { get; private set; }

    public Expression Process(Expression expression)
    {
      RootExpression = expression;

      if (MemberMapper.Options.Safety.PerformNullChecksOnCustomMappings)
      {
        var memberVisitor = new MemberVisitor(MemberMapper);

        // Pass 1: Transform member access so they do null-checks first, if needed
        expression = memberVisitor.Visit(expression);
        RootExpression = expression;
      }

      var paramVisitor = new ParameterVisitor(this.ParametersToReplace);

      // Pass 2: Transform parameter placeholders with their final ones
      expression = paramVisitor.Visit(expression);

      RootExpression = expression;

      var visibilityVisitor = new VisibilityVisitor(this);

      // Pass 3: Check visibility of what is accessed, to establish if we can compile to a new dynamic assembly.
      visibilityVisitor.Visit(expression);

      RootExpression = expression;

      this.NonPublicMembersAccessed = visibilityVisitor.NonPublicMembersAccessed;

      if (this.NonPublicMembersAccessed)
      {
        var lambdaVisitor = new LambdaVisitor(this);

        expression = lambdaVisitor.Visit(expression);

        RootExpression = expression;
      }

      return expression;
    }

    private class ParameterVisitor : ExpressionVisitor
    {
      private ICollection<ExpressionTuple> parameters;

      public ParameterVisitor(ICollection<ExpressionTuple> parameters)
      {
        this.parameters = parameters;
      }

      private Expression ReplaceParameter(ParameterExpression parameter)
      {
        foreach (var param in parameters)
        {
          if (param.OldExpression == parameter)
          {
            return param.NewExpression;
          }
        }

        return parameter;
      }

      protected override Expression VisitParameter(ParameterExpression node)
      {
        return ReplaceParameter(node);
      }
    }


    private class MemberVisitor : ExpressionVisitor
    {
      private IMemberMapper mapper;

      public MemberVisitor(IMemberMapper mapper)
      {
        this.mapper = mapper;
      }

      private bool IsExceptionToNullCheck(MemberExpression memberNode)
      {
        var memberAccess = memberNode.Expression as MemberExpression;

        if (memberAccess != null && membersExemptFromNullCheck.Contains(memberAccess.Member))
        {
          return true;
        }

        if (memberNode.Member.Name == "Count"
          && memberNode.Member.DeclaringType.IsGenericType
          && typeof(ICollection<>).IsAssignableFrom(memberNode.Member.DeclaringType.GetGenericTypeDefinition()))
        {
          return true;
        }
        else if (memberNode.Member.Name == "Length"
          && memberNode.Member.DeclaringType == typeof(Array))
        {
          return true;
        }
        else if (memberNode.Member.DeclaringType.IsValueType)
        {
          return true;
        }


        return false;
      }

      private Expression ConvertToConditionals(Type conditionalReturnType, Expression expression, Expression newExpression)
      {

        var memberNode = expression as MemberExpression;

        if (memberNode == null)
        {
          return newExpression ?? expression;
        }

        if (newExpression == null)
        {

          if (memberNode.Expression == null || memberNode.Expression.NodeType == ExpressionType.Parameter || IsExceptionToNullCheck(memberNode))
          {
            return expression;
          }
          else
          {
            newExpression = Expression.Condition(Expression.NotEqual(memberNode.Expression, Expression.Constant(null)),
              memberNode, Expression.Default(conditionalReturnType), conditionalReturnType);
          }
        }
        else
        {

          if (memberNode.Expression == null || memberNode.Expression.NodeType == ExpressionType.Parameter)
          {
            return newExpression;
          }
          else
          {
            newExpression = Expression.Condition(Expression.NotEqual(memberNode.Expression, Expression.Constant(null)),
              newExpression, Expression.Default(conditionalReturnType), conditionalReturnType);
          }
        }

        return ConvertToConditionals(conditionalReturnType, memberNode.Expression, newExpression);

      }

      private Stack<MemberInfo> membersExemptFromNullCheck = new Stack<MemberInfo>();

      protected override Expression VisitConditional(ConditionalExpression node)
      {
        MemberInfo member = null;

        var test = node.Test as BinaryExpression;

        if (test != null && test.NodeType == ExpressionType.NotEqual)
        {
          var left = test.Left as MemberExpression;
          var right = test.Right as ConstantExpression;

          if (left != null && right != null)
          {
            member = left.Member;
            membersExemptFromNullCheck.Push(member);
          }

        }

        //if(node.Test)

        var result = base.VisitConditional(node);

        if (member != null)
        {
          membersExemptFromNullCheck.Pop();
        }

        return result;
      }

      protected override Expression VisitMember(MemberExpression node)
      {

        return ConvertToConditionals(node.Type, node, null);

      }
    }

    private class VisibilityVisitor : ExpressionVisitor
    {
      private MapExpressionProcessor processor;

      public VisibilityVisitor(MapExpressionProcessor processor)
      {
        this.processor = processor;
      }

      public bool NonPublicMembersAccessed { get; private set; }

      protected override Expression VisitConstant(ConstantExpression node)
      {
        if (!node.Type.IsPublic)
        {
          NonPublicMembersAccessed = true;

          return node;
        }

        return base.VisitConstant(node);
      }

      protected override Expression VisitMethodCall(MethodCallExpression node)
      {
        if (!node.Method.IsPublic)
        {
          NonPublicMembersAccessed = true;
          return node;
        }

        // A nested class, we're taking no chances here
        if (!node.Method.DeclaringType.IsPublic || node.Method.DeclaringType.Name.Contains("+"))
        {
          NonPublicMembersAccessed = true;
          return node;
        }

        return base.VisitMethodCall(node);
      }

      protected override Expression VisitLambda<T>(Expression<T> node)
      {
        if (this.processor.RootExpression == node)
        {
          var delType = typeof(T);

          var genericParams = delType.GetGenericArguments();

          foreach (var genericArg in genericParams)
          {
            if (!IsPublicClass(genericArg))
            {
              this.NonPublicMembersAccessed = true;
              return node;
            }
          }

        }

        return base.VisitLambda<T>(node);
      }

      private static bool IsPublicClass(Type t)
      {
        // For the purposes this method is used for, also consider generic types to be 'non-public'
        if ((!t.IsPublic && !t.IsNestedPublic) || t.IsGenericType)
        {
          return false;
        }

        int lastIndex = t.FullName.LastIndexOf('+');

        // Resolve the containing type of a nested class and check if it's public
        if (lastIndex > 0)
        {
          var containgTypeName = t.FullName.Substring(0, lastIndex);

          var containingType = Type.GetType(containgTypeName + "," + t.Assembly);

          if (containingType != null)
          {
            return containingType.IsPublic;
          }

          return false;
        }
        else
        {
          return t.IsPublic;
        }
      }
    }

    private class LambdaVisitor : ExpressionVisitor
    {
      private MapExpressionProcessor processor;

      public LambdaVisitor(MapExpressionProcessor processor)
      {
        this.processor = processor;
      }

      protected override Expression VisitLambda<T>(Expression<T> node)
      {

        if (node != processor.RootExpression)
        {
          return Expression.Constant(node.Compile());
        }

        return base.VisitLambda<T>(node);
      }
    }

  }
}
