using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public class StackTraceSettings : ScriptableObject
	{
		public enum PathDisplay
		{
			Hidden,
			Visible,
			OnlyIfExist
		}

		public enum DisplayReflectedType
		{
			NamespaceAndClass,
			Class,
			None,
		}

		[Serializable]
		public class KeywordsColor
		{
			public Color	color;
			public string[] keywords;
		}

		[Serializable]
		public class MethodCategory
		{
			public string	method;
			public string	category;
		}

		[LocaleHeader("StackTrace_Filters")]
		public List<string>	filters = new List<string>();
		[LocaleHeader("StackTrace_Categories")]
		public List<MethodCategory>	categories = new List<MethodCategory>();
		[LocaleHeader("StackTrace_DisplayFilepath")]
		public PathDisplay	displayFilepath = PathDisplay.OnlyIfExist;
		[LocaleHeader("StackTrace_SkipUnreachableFrame")]
		public bool			skipUnreachableFrame = true;
		[LocaleHeader("StackTrace_DisplayRelativeToAssets")]
		public bool			displayRelativeToAssets = true;
		[LocaleHeader("StackTrace_PingFolderOnModifier")]
		public EventModifiers	pingFolderOnModifier = EventModifiers.Control;
		[LocaleHeader("StackTrace_Style"), SerializeField]
		internal GUIStyleOverride	styleOverride = new GUIStyleOverride() { baseStyleName = "label" };
		public GUIStyle				Style { get { return this.styleOverride.GetStyle(); } set { } }
		[LocaleHeader("StackTrace_Height")]
		public float		height = ConsoleConstants.DefaultSingleLineHeight;
		[LocaleHeader("StackTrace_DisplayReturnValue")]
		public bool			displayReturnValue = true;
		[LocaleHeader("StackTrace_IndentAfterReturnType")]
		public bool			indentAfterReturnType = true;
		[LocaleHeader("StackTrace_ReturnValueColor")]
		public Color		returnValueColor = new Color(78F / 255F, 50F / 255F, 255F / 255F);
		[LocaleHeader("StackTrace_DisplayReflectedType")]
		public DisplayReflectedType	displayReflectedType = DisplayReflectedType.Class;
		[LocaleHeader("StackTrace_ReflectedTypeColor")]
		public Color		reflectedTypeColor = new Color(103F / 255F, 103F / 255F, 103F / 255F);
		[LocaleHeader("StackTrace_MethodNameColor")]
		public Color		methodNameColor = new Color(149F / 255F, 68F / 255F, 84F / 255F);
		[LocaleHeader("StackTrace_DisplayArgumentType")]
		public bool			displayArgumentType = true;
		[LocaleHeader("StackTrace_ArgumentTypeColor")]
		public Color		argumentTypeColor = new Color(84F / 255F, 40F / 255, 250F / 255F);
		[LocaleHeader("StackTrace_DisplayArgumentName")]
		public bool			displayArgumentName = true;
		[LocaleHeader("StackTrace_ArgumentNameColor")]
		public Color		argumentNameColor = new Color(170F / 255F, 115F / 255F, 114F / 255F);
		[LocaleHeader("StackTrace_IndentAfterArgument")]
		public bool			indentAfterArgument = true;
		[LocaleHeader("StackTrace_FilepathColor")]
		public Color		filepathColor = new Color(69F / 255F, 69F / 255F, 69F / 255F);
		[LocaleHeader("StackTrace_LineColor")]
		public Color		lineColor = new Color(44F / 255F, 6F / 255F, 199F / 255F);
		[LocaleHeader("StackTrace_PreviewOffset")]
		public Vector2		previewOffset = new Vector2(-15F, 15F);
		[LocaleHeader("StackTrace_PreviewLinesBeforeStackFrame")]
		public int			previewLinesBeforeStackFrame = 3;
		[LocaleHeader("StackTrace_PreviewLinesAfterStackFrame")]
		public int			previewLinesAfterStackFrame = 3;
		[LocaleHeader("StackTrace_DisplayTabAsSpaces")]
		public int			displayTabAsSpaces = 4;
		[LocaleHeader("StackTrace_PreviewTextColor")]
		public Color		previewTextColor = new Color(68F / 255F, 69F / 255F, 69F / 255F);
		[LocaleHeader("StackTrace_PreviewLineColor")]
		public Color		previewLineColor = new Color(44F / 255F, 6F / 255F, 199F / 255F);
		public Color		previewSourceCodeBackgroundColor = new Color(174F / 255F, 177F / 255F, 177F / 255F);
		public Color		previewSourceCodeMainLineBackgroundColor = new Color(161F / 255F, 161F / 255F, 161F / 255F);
		[LocaleHeader("StackTrace_PreviewHeight")]
		public float		previewHeight = 16F;
		[SerializeField]
		internal GUIStyleOverride	previewSourceCodeStyleOverride = new GUIStyleOverride() { baseStyleName = "label" };
		public GUIStyle				PreviewSourceCodeStyle { get { return this.previewSourceCodeStyleOverride.GetStyle(); } set { } }
		[LocaleHeader("StackTrace_Keywords")]
		public KeywordsColor[]	keywords = new KeywordsColor[] {
			new KeywordsColor() {
				color = new Color(96F / 255F, 69F / 255F, 0F / 255F),
				keywords = new string[] { ";", "=", ".", "{", "}", "(", ")", ",", "[", "]", "+", "-", "*", "/", ":", "!", "?", "@", "<", ">" },
			},
			new KeywordsColor() {
				color = new Color(141F / 255F, 47F / 255F, 212F / 255F),
				keywords = new string[] { "this", "if", "else", "new", "foreach", "for", "switch", "while", "as", "is", "get", "set", "try", "catch", "finally", "return", "yield", "public", "private", "protected", "static", "throw", "internal", "virtual", "override", "base", "implicit", "explicit", "ref", "out", "in", "using", "class", "struct", "where", "params", "elif", "endif" },
			},
			new KeywordsColor() {
				color = new Color(186F / 255F, 99F / 255F, 218F / 255F),
				keywords = new string[] { "var", "void", "float", "uint", "int", "string", "object", "bool", "type", "char", "sbyte", "byte", "double", "decimal", "ulong", "long", "ushort", "short", "Boolean", "Char", "String", "SByte", "Byte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "Single", "Double", "Decimal" },
			},
		};
	}
}