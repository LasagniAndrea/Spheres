using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using EFS.Referential;

namespace EFS.Spheres
{
	/// <summary>
	/// Description r�sum�e de Referential.
	/// </summary>
    public partial class ReferentialPage : PageBaseReferentialv2
	{
	
		protected void Page_Load(object sender, System.EventArgs e)
		{
            //20071212 FI Ticket 16012 => Migration Asp2.0
            //SmartNavigation = false;
		}
		#region Code g�n�r� par le Concepteur Web Form
        protected override void OnInit(EventArgs e)
		{
			//
			// CODEGEN�: Cet appel est requis par le Concepteur Web Form ASP.NET.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		/// <summary>
		/// M�thode requise pour la prise en charge du concepteur - ne modifiez pas
		/// le contenu de cette m�thode avec l'�diteur de code.
		/// </summary>
		private void InitializeComponent()
		{    
		}
		#endregion
	}
}
