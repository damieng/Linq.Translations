# Microsoft.Linq.Translations (also known as Expressives)

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Linq.Translations.svg?style=flat)](https://www.nuget.org/packages/Microsoft.Linq.Translations/)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/6dvhu9u63ldp5s4j?svg=true)](https://ci.appveyor.com/project/damieng/linq-translations)

## Overview

If you’ve ever written a property on your class and then got annoyed you can’t use your locally-declared property as part of a remote query selection this library is for you.

The problem occurs because these properties can’t be translated and sent to the server as they have been compiled into intermediate language (IL) and not LINQ expression trees that are required for translation by IQueryable implementations. There is nothing available in .NET to let us reverse-engineer the IL back into the methods and syntax that would allow us to translate the intended operation into a remote query.

This means you end up having to write your query in two parts; firstly the part the server can do, a ToList or AsEnumerable call to force that to happen and bring the intermediate results down to the client, and then the operations that can only be evaluated locally. This can hurt performance if you want to reduce or transform the result set significantly.

### Before example

Here we have extended the Employee class to add Age and FullName. We only wanted to people with “da” in their name but we are forced to pull down everything to the client in order to the do the selection.

```csharp
partial class Employee {
  public string FullName {
    get { return Forename + " " + Surname; }
  }

  public int Age {
    get { return DateTime.Now.Year - BirthDate.Year - (((DateTime.Now.Month < BirthDate.Month)
			|| DateTime.Now.Month == BirthDate.Month && DateTime.Now.Day < BirthDate.Day) ? 1 : 0));
    }
  }
}

var employees = db.Employees.ToList().Where(e => e.FullName.Contains("da")).GroupBy(e => e.Age);
```

### After example

Here using Microsoft.Linq.Translations it all happens server side and works on both LINQ to Entities and LINQ to SQL.

```csharp
partial class Employee {
    private static readonly CompiledExpression<Employee,string> fullNameExpression
     = DefaultTranslationOf<Employee>.Property(e => e.FullName).Is(e => e.Forename + " " + e.Surname);
    private static readonly CompiledExpression<Employee,int> ageExpression
     = DefaultTranslationOf<Employee>.Property(e => e.Age).Is(e => DateTime.Now.Year - e.BirthDate.Value.Year - (((DateTime.Now.Month < e.BirthDate.Value.Month) || (DateTime.Now.Month == e.BirthDate.Value.Month && DateTime.Now.Day < e.BirthDate.Value.Day)) ? 1 : 0)));

  public string FullName {
    get { return fullNameExpression.Evaluate(this); }
  }

  public int Age {
    get { return ageExpression.Evaluate(this); }
  }
}

var employees = db.Employees.Where(e => e.FullName.Contains("da")).GroupBy(e => e.Age).WithTranslations();
```

## Getting started

Grab the package from <a href="http://nuget.org/List/Packages/Microsoft.Linq.Translations">NuGet</a> or <a href="https://github.com/damieng/Linq.Translations">Source on GitHub</a>.

### Usage considerations

The caveats to the usage technique shown above is you need to ensure your class has been initialized before you write queries to it (check out alternatives below) and obviously the expression you register for a property must be able to be translated to the remote store so you will need to constrain yourself to the methods and operators your IQueryable provider supports.

There are a few alternative ways to use this rather than the specific examples above.

### Registering the expressions

You can register the properties in the class itself as shown in the examples which means the properties themselves can evaluate the expressions without any reflection calls. Alternatively if performance is less critical you can register them elsewhere and have the methods look up their values dynamically via reflection. e.g.

```csharp
DefaultTranslationOf<Employee>.Property(e => e.FullName).Is(e => e.Forename + " " + e.Surname);
var employees = db.Employees.Where(e => e.FullName.Contains("da")).GroupBy(e => e.Age).WithTranslations();

partial class Employee {
    public string FullName { get { return DefaultTranslationOf<Employees>.Evaluate<string>(this, MethodInfo.GetCurrentMethod());} }
}
```

If performance of the client-side properties is critical then you can always have them as regular get properties with the full code in there at the expense of having the calculation duplicated, once in IL in the property and once as an expression for the translation.

### Different maps for different scenarios

Sometimes certain parts of your application may want to run with different translations for different scenarios, performance etc. No problem!

The WithTranslations method normally operates against the default translation map (accessed with DefaultTranslationOf) but there is also another overload that takes a TranslationMap you can build for specific scenarios, e.g.

```csharp
var myTranslationMap = new TranslationMap();
myTranslationMap.Add<Employees, string>(e => e.Name, e => e.FirstName + " " + e.LastName);
var results = (from e in db.Employees where e.Name.Contains("martin") select e).WithTranslations(myTranslationMap).ToList();
```

## How it works

### CompiledExpression<T, TResult>

The first thing we needed to do was get the user-written client-side “computed” properties out of IL and back into expression trees so we could translate them. Given that we also want to evaluate them on the client we need to compile them at run time so CompiledExpression exists which just takes an expression of Func<T, TResult>, compiles it and allows evaluation of objects against the compiled version.

### ExpressiveExtensions

This little class provides both the WithTranslations extensions methods and the internal TranslatingVisitor that unravels the property accesses into their actual registered Func<T, TResult> expressions via the TranslationMap so that the underlying LINQ provider can deal with that instead.

### TranslationMap

We need to have a map of properties to compiled expressions and for that purpose TranslationMap exists. You can create a TranslationMap by hand and pass it in to WithTranslations if you want to programmatically create them at run-time or have different ones for different scenarios but generally you will want to use…

### DefaultTranslationOf

This helper class lets you register properties against the default TranslationMap we use when nothing is passed to WithTranslations. It also allows you to lookup what is already registered so you can evaluate to that although there is a small reflection performance penalty for that:

```csharp
public int Age { get { return DefaultTranslationOf<Employees>.Evaluate<int>(this, MethodInfo.GetCurrentMethod()); } }
```
