using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

internal static class Program
{
    private static int Main()
    {
        int passed = 0, failed = 0;
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "Surlhwa.Tests"))
        foreach (MethodInfo method in type.GetMethods())
        {
            var cases = method.GetCustomAttributes<TestCaseAttribute>().ToArray();
            if (cases.Length == 0 && method.GetCustomAttribute<TestAttribute>() == null) continue;
            object[][] inputs = cases.Length == 0 ? new[] { Array.Empty<object>() } : cases.Select(c => c.Arguments).ToArray();
            foreach (object[] input in inputs)
            {
                string name = method.Name + "(" + string.Join(",", input) + ")";
                try { method.Invoke(Activator.CreateInstance(type), input); Console.WriteLine("PASS " + name); passed++; }
                catch (Exception e) { Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); failed++; }
            }
        }
        Console.WriteLine($"RESULT {passed} passed, {failed} failed — standalone .NET contract tests, NOT Unity execution");
        return failed == 0 ? 0 : 1;
    }
}

