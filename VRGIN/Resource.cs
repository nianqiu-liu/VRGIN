using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace VRGIN
{
	[global::System.CodeDom.Compiler.GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
	[global::System.Diagnostics.DebuggerNonUserCode]
	[global::System.Runtime.CompilerServices.CompilerGenerated]
	internal class Resource
	{
		internal Resource()
		{
		}

		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Advanced)]
		internal static global::System.Globalization.CultureInfo Culture
		{
			get
			{
				return global::VRGIN.Resource.resourceCulture;
			}
			set
			{
				global::VRGIN.Resource.resourceCulture = value;
			}
		}

		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Advanced)]
		internal static global::System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (global::VRGIN.Resource.resourceMan == null)
				{
					global::VRGIN.Resource.resourceMan = new global::System.Resources.ResourceManager("VRGIN.Resource", typeof(global::VRGIN.Resource).Assembly);
				}
				return global::VRGIN.Resource.resourceMan;
			}
		}

		internal static byte[] steamvr_2019
		{
			get
			{
				return (byte[])global::VRGIN.Resource.ResourceManager.GetObject("steamvr_2019", global::VRGIN.Resource.resourceCulture);
			}
		}

		private static global::System.Globalization.CultureInfo resourceCulture;

		private static global::System.Resources.ResourceManager resourceMan;
	}
}
