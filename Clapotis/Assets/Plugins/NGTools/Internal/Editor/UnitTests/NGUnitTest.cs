using NGTools;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace NGToolsEditor.Internal
{
	public static class NGAssert
	{
		public static void	AssertBoolTrue(bool result, string label)
		{
			NGUnitTestsManager.AddResult(result == true, label);
		}
	}

	public sealed class TestMethodAttribute : Attribute
	{
	}

	public abstract class NGUnitTest
	{
	}

	public class TestExposers : NGUnitTest
	{
		[TestMethod]
		public void	Test()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(ComponentExposer)))
			{
				ComponentExposer	test = Activator.CreateInstance(type) as ComponentExposer;
				FieldInfo[]			fields = test.GetFieldInfos();

				InternalNGDebug.Log("Testing exposer " + type.Name);

				for (int i = 0; i < fields.Length; i++)
					NGAssert.AssertBoolTrue(fields[i] != null && fields[i].IsDefined(typeof(ObsoleteAttribute), false) == false, "Field " + i + " is good.");

				PropertyInfo[]	properties = test.GetPropertyInfos();

				for (int i = 0; i < properties.Length; i++)
					NGAssert.AssertBoolTrue(properties[i] != null && properties[i].IsDefined(typeof(ObsoleteAttribute), false) == false, "Property " + i + " is good.");
			}
		}
	}

	public class TestMetrics : NGUnitTest
	{
		[TestMethod]
		public void	TestConstantsDuplicate()
		{
			Type		type = typeof(Metrics);
			FieldInfo[]	fields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
			int			constants = 0;

			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].IsLiteral == false || fields[i].FieldType != typeof(int))
					continue;

				int	v = (int)fields[i].GetRawConstantValue();

				if ((constants & (1 << (v - 1))) == 0)
				{
					constants |= (1 << (v - 1));
					NGAssert.AssertBoolTrue(true, "Field " + fields[i].Name + " (" + v + ") is valid.");
				}
				else
					NGAssert.AssertBoolTrue(false, "Field " + fields[i].Name + " (" + v + ") is valid.");
			}
		}
	}

	public class NGUnitTestsManager : EditorWindow
	{
		public struct TestResult
		{
			public bool		succeed;
			public string	label;
		}

		//private static List<TestResult>	results = new List<TestResult>();

		public string	outputFilepath;

		private List<NGUnitTest>	tests = new List<NGUnitTest>();

		[MenuItem(Constants.PackageTitle + "/Internal/Launch all tests")]
		private static void	LaunchAllTests()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(NGUnitTest)))
			{
				NGUnitTest		test = Activator.CreateInstance(type) as NGUnitTest;
				MethodInfo[]	methods = type.GetMethods();

				InternalNGDebug.Log("Testing " + type.Name);

				for (int i = 0; i < methods.Length; i++)
				{
					if (methods[i].IsDefined(typeof(TestMethodAttribute), false) == true)
					{
						try
						{
							methods[i].Invoke(test, null);
						}
						catch (Exception ex)
						{
							InternalNGDebug.LogException(ex);
						}
					}
				}
			}
		}

		public static void	AddResult(bool result, string label)
		{
			if (result == true)
				InternalNGDebug.Log("<color=green>" + label + "</color>");
			else
				InternalNGDebug.LogError("<color=red>" + label + "</color>");
		}

		protected virtual void	OnEnable()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(NGUnitTest)))
				this.tests.Add(Activator.CreateInstance(type) as NGUnitTest);
		}

		protected virtual void	OnGUI()
		{

		}
	}
}