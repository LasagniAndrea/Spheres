﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :2.0.50727.4959
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EFS.SpheresRiskPerformance.SpheresObjects
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[System.Data.Linq.Mapping.DatabaseAttribute(Name="RD_SPHERES_TST260")]
	public partial class MARGINDETTRACKDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    #endregion
		
		public MARGINDETTRACKDataContext() : 
				base(global::EFS.SpheresRiskPerformance.Properties.Settings.Default.RD_SPHERES_TST260ConnectionString1, mappingSource)
		{
			OnCreated();
		}
		
		public MARGINDETTRACKDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public MARGINDETTRACKDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public MARGINDETTRACKDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public MARGINDETTRACKDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<MARGINDETTRACK> MARGINDETTRACK
		{
			get
			{
				return this.GetTable<MARGINDETTRACK>();
			}
		}
	}
	
	[Table(Name="dbo.MARGINDETTRACK")]
	public partial class MARGINDETTRACK
	{
		
		private decimal _IDMARGINTRACK;
		
		private System.Nullable<decimal> _IDT;
		
		private string _MARGINXML;
		
		public MARGINDETTRACK()
		{
		}
		
		[Column(Storage="_IDMARGINTRACK", DbType="Decimal(10,0) NOT NULL")]
		public decimal IDMARGINTRACK
		{
			get
			{
				return this._IDMARGINTRACK;
			}
			set
			{
				if ((this._IDMARGINTRACK != value))
				{
					this._IDMARGINTRACK = value;
				}
			}
		}
		
		[Column(Storage="_IDT", DbType="Decimal(10,0)")]
		public System.Nullable<decimal> IDT
		{
			get
			{
				return this._IDT;
			}
			set
			{
				if ((this._IDT != value))
				{
					this._IDT = value;
				}
			}
		}
		
		[Column(Storage="_MARGINXML", DbType="Xml NOT NULL", CanBeNull=false, UpdateCheck=UpdateCheck.Never)]
        public string MARGINXML
		{
			get
			{
				return this._MARGINXML;
			}
			set
			{
				if ((this._MARGINXML != value))
				{
					this._MARGINXML = value;
				}
			}
		}
	}
}
#pragma warning restore 1591
