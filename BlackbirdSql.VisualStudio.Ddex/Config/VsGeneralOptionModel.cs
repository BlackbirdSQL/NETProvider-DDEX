﻿// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)


using System.ComponentModel;
using BlackbirdSql.Core.Options;

namespace BlackbirdSql.VisualStudio.Ddex.Config
{


	// =========================================================================================================
	//										VsGeneralOptionModel Class
	//
	/// <summary>
	/// Option Model for General options
	/// </summary>
	// =========================================================================================================
	public class VsGeneralOptionModel : AbstractOptionModel<VsGeneralOptionModel>
	{

		[Category("Diagnostics")]
		[DisplayName("Enable diagnostics")]
		[Description("Enables the execution of Diagnostics calls. This option should remain enabled. Disabling does not disable Exceptions.")]
		[DefaultValue(true)]
		public bool EnableDiagnostics { get; set; } = true;

		[Category("Diagnostics")]
		[DisplayName("Enable task logging")]
		[Description("Enables logging of tasks to the output window pane. This option should remain enabled.")]
		[DefaultValue(true)]
		public bool EnableTaskLog { get; set; } = true;

		[Category("EntityFramework settings")]
		[DisplayName("Validate App.config")]
		[Description("Enable this option to allow BlackbirdSql to ensure that Firebird EntityFramework is configured in your non-CPS projects' App.config.")]
		[DefaultValue(true)]
		public bool ValidateConfig { get; set; } = true;

		[Category("EntityFramework settings")]
		[DisplayName("Update legacy edmx models")]
		[Description("Enable this option to allow BlackbirdSql to update legacy edmx models to use EntityFramework 6.")]
		[DefaultValue(true)]
		public bool ValidateEdmx { get; set; } = true;


		public VsGeneralOptionModel() : base("General")
		{
		}

	}
}