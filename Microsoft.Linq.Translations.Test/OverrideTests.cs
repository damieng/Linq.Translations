using Microsoft.Linq.Translations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltafs.Platinum.InMemoryDatabase.IntegrationTests
{
  /// <summary>
  ///  Microsoft.Liq.Translations is a third party linrary we use to simplify the definition of Linq-to-entities friendly
  ///  calculated fields in entities, we have made some minor enhancements to the library these tests test those enhancements
  /// </summary>
  [TestFixture]
  public class OverrideTests
  {

    public class DemoBaseClass
    {
      [Key]
      public Int32 pk { get; set; }
      public virtual String NormalAttribute { get; set; }
      public virtual String CalculatedAttribute
      {
        get
        {
          return expCalculatedAttribute.Evaluate(this);
        }
      }

      private static CompiledExpression<DemoBaseClass, String> expCalculatedAttribute =
        DefaultTranslationOf<DemoBaseClass>.Property(p => p.CalculatedAttribute)
          .Is(o => o.NormalAttribute);
    }

    public class DemoDerivedClass1 : DemoBaseClass
    {
      public override string NormalAttribute
      {
        get
        {
          return "Override 1";
        }
        set
        {
          base.NormalAttribute = value;
        }
      }

      public override string CalculatedAttribute
      {
        get
        {
          return expDerivedClassOneCalculation.Evaluate(this);
        }
      }

      private static CompiledExpression<DemoDerivedClass1, String> expDerivedClassOneCalculation =
        DefaultTranslationOf<DemoDerivedClass1>.Property(p => p.CalculatedAttribute)
          .Is(o => "Calculated Override 1");

    }

    public class DemoDerivedClass2 : DemoDerivedClass1
    {
      public override string NormalAttribute
      {
        get
        {
          return "Override 2";
        }
        set
        {
          base.NormalAttribute = value;
        }
      }
    }

    public class DemoDerivedClass3 : DemoBaseClass
    {
      public override string NormalAttribute
      {
        get
        {
          return "Override 3";
        }
        set
        {
          base.NormalAttribute = value;
        }
      }
    }

    public class DemoContext : DbContext
    {
      public DemoContext(DbConnection connection)
        : base(connection, true)
      {
      }

      public virtual DbSet<DemoBaseClass> DemoBaseClasses { get; set; }
      public virtual DbSet<DemoDerivedClass1> DemoDerivedClassOnes { get; set; }
      public virtual DbSet<DemoDerivedClass2> DemoDerivedClassTwos { get; set; }
      public virtual DbSet<DemoDerivedClass3> DemoDerivedClassThrees { get; set; }
    }

    private void SeedContext(DemoContext context)
    {
      //seed some data in
      var demo = new DemoBaseClass() { NormalAttribute = "TEST" };
      context.DemoBaseClasses.Add(demo);

      var demo1 = new DemoDerivedClass1() { NormalAttribute = "TEST1" };
      context.DemoDerivedClassOnes.Add(demo1);

      var demo2 = new DemoDerivedClass2() { NormalAttribute = "TEST2" };
      context.DemoDerivedClassTwos.Add(demo2);

      var demo3 = new DemoDerivedClass3() { NormalAttribute = "TEST3" };
      context.DemoDerivedClassThrees.Add(demo3);

      context.SaveChanges();
    }

    [Test]
    public void LinqTranslations_NormalAttribute_NormalOverrideBehaviour()
    {
      using (var connection = Effort.DbConnectionFactory.CreateTransient())
      {
        using (var context = new DemoContext(connection))
        {
          SeedContext(context);
        }

        using (var context = new DemoContext(connection))
        {
          var result = context.DemoBaseClasses.First().NormalAttribute;
          Assert.AreEqual("TEST", result);

          var result1 = context.DemoDerivedClassOnes.First().NormalAttribute;
          Assert.AreEqual("Override 1", result1);

          var result2 = context.DemoDerivedClassTwos.First().NormalAttribute;
          Assert.AreEqual("Override 2", result2);

          var result3 = context.DemoDerivedClassThrees.First().NormalAttribute;
          Assert.AreEqual("Override 3", result3);
        }
      }
    }

    [Test]
    public void LinqTranslations_CalculatedAttribute_OverrideBehaviour()
    {
      using (var connection = Effort.DbConnectionFactory.CreateTransient())
      {
        using (var context = new DemoContext(connection))
        {
          SeedContext(context);
        }

        using (var context = new DemoContext(connection))
        {
          var qry = from m in context.DemoBaseClasses
                    select m.CalculatedAttribute;
          Assert.AreEqual("TEST", qry.WithTranslations().First());

          var qry1 = from m in context.DemoDerivedClassOnes
                     select m.CalculatedAttribute;
          Assert.AreEqual("Calculated Override 1", qry1.WithTranslations().First());

          var qry2 = from m in context.DemoDerivedClassTwos
                     select m.CalculatedAttribute;
          Assert.AreEqual("Calculated Override 1", qry2.WithTranslations().First());

          var qry3 = from m in context.DemoDerivedClassThrees
                     select m.CalculatedAttribute;
          Assert.AreEqual("Override 3", qry3.WithTranslations().First());

        }
      }
    }
  }

}
