﻿// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)

using System;
using System.ComponentModel;
using System.Drawing.Design;
using BlackbirdSql.Core.Ctl.ComponentModel;
using BlackbirdSql.Core.Model.Config;
using BlackbirdSql.EditorExtension.Controls.ComponentModel;
using BlackbirdSql.EditorExtension.Ctl.ComponentModel;
using BlackbirdSql.Sys.Interfaces;

using GlobalizedCategoryAttribute = BlackbirdSql.EditorExtension.Ctl.ComponentModel.GlobalizedCategoryAttribute;
using GlobalizedDescriptionAttribute = BlackbirdSql.EditorExtension.Ctl.ComponentModel.GlobalizedDescriptionAttribute;
using GlobalizedDisplayNameAttribute = BlackbirdSql.EditorExtension.Ctl.ComponentModel.GlobalizedDisplayNameAttribute;



namespace BlackbirdSql.EditorExtension.Model.Config;


// =========================================================================================================
//										ResultsSettingsModel Class
//
/// <summary>
/// Option Model for General Results options
/// </summary>
// =========================================================================================================
public class ResultsSettingsModel : AbstractSettingsModel<ResultsSettingsModel>
{

	// ---------------------------------------------------------------------------------
	#region Constructors / Destructors - ResultsSettingsModel
	// ---------------------------------------------------------------------------------


	public ResultsSettingsModel() : this(null)
	{
	}


	public ResultsSettingsModel(IBsSettingsProvider transientSettings)
		: base(C_Package, C_Group, C_PropertyPrefix, transientSettings)
	{
	}


	#endregion Constructors / Destructors




	// =====================================================================================================
	#region Constants - ResultsSettingsModel
	// =====================================================================================================


	private const string C_Package = "Editor";
	private const string C_Group = "Results";
	private const string C_PropertyPrefix = "EditorResultsGeneral";


	#endregion Constants




	// =====================================================================================================
	#region Property Accessors - ResultsSettingsModel
	// =====================================================================================================


	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override string CollectionName { get; } = "\\BlackbirdSql\\SqlEditor.ResultsSettings";



	[TypeConverter(typeof(GlobalEnumConverter))]
	public enum EnGlobalizedSqlOutputMode
	{
		[GlobalizedRadio("EnSqlOutputMode_Grid")]
		ToGrid,
		[GlobalizedRadio("EnSqlOutputMode_Text")]
		ToText,
		[GlobalizedRadio("EnSqlOutputMode_File")]
		ToFile
	}

	// General section

	[GlobalizedCategory("OptionCategoryGeneral")]
	[GlobalizedDisplayName("OptionDisplayResultsOutputMode")]
	[GlobalizedDescription("OptionDescriptionResultsOutputMode")]
	[DefaultValue(EnGlobalizedSqlOutputMode.ToGrid)]
	public EnGlobalizedSqlOutputMode OutputMode { get; set; } = EnGlobalizedSqlOutputMode.ToGrid;

	[GlobalizedCategory("OptionCategoryGeneral")]
	[GlobalizedDisplayName("OptionDisplayResultsDirectory")]
	[GlobalizedDescription("OptionDescriptionResultsDirectory")]
	[Editor(typeof(AdvancedFolderNameEditor), typeof(UITypeEditor)), Parameters("OptionDialogResultsDirectory")]
	[AdvancedDefaultValue(typeof(Environment.SpecialFolder), Environment.SpecialFolder.Personal)]
	public string Directory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

	[GlobalizedCategory("OptionCategoryGeneral")]
	[GlobalizedDisplayName("OptionDisplayResultsPlaySounds")]
	[GlobalizedDescription("OptionDescriptionResultsPlaySounds")]
	[TypeConverter(typeof(GlobalEnableDisableConverter))]
	[DefaultValue(false)]
	public bool PlaySounds { get; set; } = false;


	#endregion Property Accessors

}
