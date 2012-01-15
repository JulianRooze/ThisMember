﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThisMember.Core;

namespace ThisMember.Test
{
  [TestClass]
  public class FlattenHierarchyTests
  {
    class CompanyContainer
    {
      public Company Company { get; set; }
    }

    class Company
    {
      public int ID { get; set; }
    }

    class CompanyDto
    {
      public int CompanyID { get; set; }
    }

    [TestMethod]
    public void FlattenHierarchyWorks()
    {
      var mapper = new MemberMapper();

      var result = mapper.Map(new CompanyContainer { Company = new Company { ID = 10 } }, new CompanyDto());

      Assert.AreEqual(10, result.CompanyID);
    }

    [TestMethod]
    public void FlattenHierarchyWorksWithNullValues()
    {
      var mapper = new MemberMapper();

      var result = mapper.Map(new CompanyContainer { Company = null }, new CompanyDto());

      Assert.AreEqual(0, result.CompanyID);
    }

    [TestMethod]
    public void FlattenHierarchyWorksWithComplexType()
    {
      var mapper = new MemberMapper();

      var result = mapper.Map(new CompanyContainer { Company = new Company { ID = 10 } }, new CompanyDto());

      Assert.AreEqual(10, result.CompanyID);
    }

    class Layer0
    {
      public Layer1 One { get; set; }
    }

    class Layer1
    {
      public Layer2 Two { get; set; }
    }

    class Layer2
    {
      public string Value { get; set; }
    }

    class LayerDestination
    {
      public string OneTwoValue { get; set; }
    }
    [TestMethod]
    public void FlattenHierarchyWorksForDeeperHierarchies()
    {
      var mapper = new MemberMapper();

      var result = mapper.Map(new Layer0
        {
          One = new Layer1
          {
            Two = new Layer2
            {
              Value = "Test"
            }
          }
        }, new LayerDestination());

      Assert.AreEqual("Test", result.OneTwoValue);
    }

    [TestMethod]
    public void FlattenHierarchyWorksForDeeperHierarchiesAndNullValues_1()
    {
      var mapper = new MemberMapper();

      var result = mapper.Map(new Layer0
      {
        One = null
      }, new LayerDestination());

      Assert.IsNull(result.OneTwoValue);
    }

    [TestMethod]
    public void FlattenHierarchyWorksForDeeperHierarchiesAndNullValues_2()
    {
      var mapper = new MemberMapper();

      var result = mapper.Map(new Layer0
      {
        One = new Layer1
        {
          Two = null
        }
      }, new LayerDestination());

      Assert.IsNull(result.OneTwoValue);
    }
  }
}