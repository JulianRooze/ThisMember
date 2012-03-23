﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using ThisMember.Core.Interfaces;
using ThisMember.Core.Misc;

namespace ThisMember.Core.Fluent
{
  public class SourceTypeModifier<TSource>
  {
    private IMemberMapper mapper;

    public SourceTypeModifier(IMemberMapper mapper)
    {
      this.mapper = mapper;
    }

    public void ThrowIf(LambdaExpression condition, string message)
    {
      if (condition == null) throw new ArgumentNullException("condition");

      if (condition.Parameters.Count != 1 || condition.Parameters.Single().Type != typeof(TSource))
      {
        throw new InvalidOperationException("Invalid expression parameters");
      }

      if (condition.ReturnType != typeof(bool))
      {
        throw new InvalidOperationException("Invalid return type, must be bool");
      }

      var data = new SourceTypeData
      {
        Type = typeof(TSource),
        Message = message,
        ThrowIfCondition = condition
      };

      mapper.Data.AddSourceTypeData(data);
    }

    public void ThrowIf(Expression<Func<TSource, bool>> condition, string message)
    {
      if (condition == null) throw new ArgumentNullException("condition");

      ThrowIf((LambdaExpression)condition, message);
    }
  }
}
