﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// Ce code source a été automatiquement généré par Microsoft.VSDesigner, Version 4.0.30319.42000.
// 
#pragma warning disable 1591

namespace FlyDocLibrary.SessionService {
    using System;
    using System.Web.Services;
    using System.Diagnostics;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    using System.ComponentModel;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="SessionServiceSoap", Namespace="urn:SessionService")]
    public partial class SessionService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback GetBindingsOperationCompleted;
        
        private SessionHeader sessionHeaderValueField;
        
        private System.Threading.SendOrPostCallback LoginOperationCompleted;
        
        private System.Threading.SendOrPostCallback LogoutOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetServiceInformationOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetSessionInformationOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public SessionService() {
            this.Url = global::FlyDocLibrary.Properties.Settings.Default.FlyDoc_FlyDocLibrary_SessionService_SessionService;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public SessionHeader SessionHeaderValue {
            get {
                return this.sessionHeaderValueField;
            }
            set {
                this.sessionHeaderValueField = value;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event GetBindingsCompletedEventHandler GetBindingsCompleted;
        
        /// <remarks/>
        public event LoginCompletedEventHandler LoginCompleted;
        
        /// <remarks/>
        public event LogoutCompletedEventHandler LogoutCompleted;
        
        /// <remarks/>
        public event GetServiceInformationCompletedEventHandler GetServiceInformationCompleted;
        
        /// <remarks/>
        public event GetSessionInformationCompletedEventHandler GetSessionInformationCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("#GetBindings", RequestNamespace="urn:SessionService", ResponseNamespace="urn:SessionService", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("return")]
        public BindingResult GetBindings(string reserved) {
            object[] results = this.Invoke("GetBindings", new object[] {
                        reserved});
            return ((BindingResult)(results[0]));
        }
        
        /// <remarks/>
        public void GetBindingsAsync(string reserved) {
            this.GetBindingsAsync(reserved, null);
        }
        
        /// <remarks/>
        public void GetBindingsAsync(string reserved, object userState) {
            if ((this.GetBindingsOperationCompleted == null)) {
                this.GetBindingsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetBindingsOperationCompleted);
            }
            this.InvokeAsync("GetBindings", new object[] {
                        reserved}, this.GetBindingsOperationCompleted, userState);
        }
        
        private void OnGetBindingsOperationCompleted(object arg) {
            if ((this.GetBindingsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetBindingsCompleted(this, new GetBindingsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("SessionHeaderValue", Direction=System.Web.Services.Protocols.SoapHeaderDirection.Out)]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("#Login", RequestNamespace="urn:SessionService", ResponseNamespace="urn:SessionService", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("return")]
        public LoginResult Login(string userName, string password) {
            object[] results = this.Invoke("Login", new object[] {
                        userName,
                        password});
            return ((LoginResult)(results[0]));
        }
        
        /// <remarks/>
        public void LoginAsync(string userName, string password) {
            this.LoginAsync(userName, password, null);
        }
        
        /// <remarks/>
        public void LoginAsync(string userName, string password, object userState) {
            if ((this.LoginOperationCompleted == null)) {
                this.LoginOperationCompleted = new System.Threading.SendOrPostCallback(this.OnLoginOperationCompleted);
            }
            this.InvokeAsync("Login", new object[] {
                        userName,
                        password}, this.LoginOperationCompleted, userState);
        }
        
        private void OnLoginOperationCompleted(object arg) {
            if ((this.LoginCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.LoginCompleted(this, new LoginCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("SessionHeaderValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("#Logout", RequestNamespace="urn:SessionService", ResponseNamespace="urn:SessionService", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void Logout() {
            this.Invoke("Logout", new object[0]);
        }
        
        /// <remarks/>
        public void LogoutAsync() {
            this.LogoutAsync(null);
        }
        
        /// <remarks/>
        public void LogoutAsync(object userState) {
            if ((this.LogoutOperationCompleted == null)) {
                this.LogoutOperationCompleted = new System.Threading.SendOrPostCallback(this.OnLogoutOperationCompleted);
            }
            this.InvokeAsync("Logout", new object[0], this.LogoutOperationCompleted, userState);
        }
        
        private void OnLogoutOperationCompleted(object arg) {
            if ((this.LogoutCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.LogoutCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("#GetServiceInformation", RequestNamespace="urn:SessionService", ResponseNamespace="urn:SessionService", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("return")]
        public ServiceInformation GetServiceInformation(string language) {
            object[] results = this.Invoke("GetServiceInformation", new object[] {
                        language});
            return ((ServiceInformation)(results[0]));
        }
        
        /// <remarks/>
        public void GetServiceInformationAsync(string language) {
            this.GetServiceInformationAsync(language, null);
        }
        
        /// <remarks/>
        public void GetServiceInformationAsync(string language, object userState) {
            if ((this.GetServiceInformationOperationCompleted == null)) {
                this.GetServiceInformationOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetServiceInformationOperationCompleted);
            }
            this.InvokeAsync("GetServiceInformation", new object[] {
                        language}, this.GetServiceInformationOperationCompleted, userState);
        }
        
        private void OnGetServiceInformationOperationCompleted(object arg) {
            if ((this.GetServiceInformationCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetServiceInformationCompleted(this, new GetServiceInformationCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("SessionHeaderValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("#GetSessionInformation", RequestNamespace="urn:SessionService", ResponseNamespace="urn:SessionService", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("return")]
        public SessionInformation GetSessionInformation() {
            object[] results = this.Invoke("GetSessionInformation", new object[0]);
            return ((SessionInformation)(results[0]));
        }
        
        /// <remarks/>
        public void GetSessionInformationAsync() {
            this.GetSessionInformationAsync(null);
        }
        
        /// <remarks/>
        public void GetSessionInformationAsync(object userState) {
            if ((this.GetSessionInformationOperationCompleted == null)) {
                this.GetSessionInformationOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetSessionInformationOperationCompleted);
            }
            this.InvokeAsync("GetSessionInformation", new object[0], this.GetSessionInformationOperationCompleted, userState);
        }
        
        private void OnGetSessionInformationOperationCompleted(object arg) {
            if ((this.GetSessionInformationCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetSessionInformationCompleted(this, new GetSessionInformationCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:SessionService")]
    [System.Xml.Serialization.XmlRootAttribute("SessionHeaderValue", Namespace="urn:SessionService", IsNullable=false)]
    public partial class SessionHeader : System.Web.Services.Protocols.SoapHeader {
        
        private string sessionIDField;
        
        /// <remarks/>
        public string sessionID {
            get {
                return this.sessionIDField;
            }
            set {
                this.sessionIDField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:SessionService")]
    public partial class SessionInformation {
        
        private string loginField;
        
        private string identifierField;
        
        private string accountField;
        
        private string nameField;
        
        private string companyField;
        
        private string emailField;
        
        private string cultureField;
        
        private string timeZoneField;
        
        private string languageField;
        
        private string filesPathField;
        
        /// <remarks/>
        public string login {
            get {
                return this.loginField;
            }
            set {
                this.loginField = value;
            }
        }
        
        /// <remarks/>
        public string identifier {
            get {
                return this.identifierField;
            }
            set {
                this.identifierField = value;
            }
        }
        
        /// <remarks/>
        public string account {
            get {
                return this.accountField;
            }
            set {
                this.accountField = value;
            }
        }
        
        /// <remarks/>
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        public string company {
            get {
                return this.companyField;
            }
            set {
                this.companyField = value;
            }
        }
        
        /// <remarks/>
        public string email {
            get {
                return this.emailField;
            }
            set {
                this.emailField = value;
            }
        }
        
        /// <remarks/>
        public string culture {
            get {
                return this.cultureField;
            }
            set {
                this.cultureField = value;
            }
        }
        
        /// <remarks/>
        public string timeZone {
            get {
                return this.timeZoneField;
            }
            set {
                this.timeZoneField = value;
            }
        }
        
        /// <remarks/>
        public string language {
            get {
                return this.languageField;
            }
            set {
                this.languageField = value;
            }
        }
        
        /// <remarks/>
        public string filesPath {
            get {
                return this.filesPathField;
            }
            set {
                this.filesPathField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:SessionService")]
    public partial class ServiceInformation {
        
        private string messageField;
        
        private string detailsField;
        
        /// <remarks/>
        public string message {
            get {
                return this.messageField;
            }
            set {
                this.messageField = value;
            }
        }
        
        /// <remarks/>
        public string details {
            get {
                return this.detailsField;
            }
            set {
                this.detailsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:SessionService")]
    public partial class LoginResult {
        
        private string sessionIDField;
        
        /// <remarks/>
        public string sessionID {
            get {
                return this.sessionIDField;
            }
            set {
                this.sessionIDField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:SessionService")]
    public partial class BindingResult {
        
        private string sessionServiceLocationField;
        
        private string submissionServiceLocationField;
        
        private string queryServiceLocationField;
        
        private string sessionServiceWSDLField;
        
        private string submissionServiceWSDLField;
        
        private string queryServiceWSDLField;
        
        /// <remarks/>
        public string sessionServiceLocation {
            get {
                return this.sessionServiceLocationField;
            }
            set {
                this.sessionServiceLocationField = value;
            }
        }
        
        /// <remarks/>
        public string submissionServiceLocation {
            get {
                return this.submissionServiceLocationField;
            }
            set {
                this.submissionServiceLocationField = value;
            }
        }
        
        /// <remarks/>
        public string queryServiceLocation {
            get {
                return this.queryServiceLocationField;
            }
            set {
                this.queryServiceLocationField = value;
            }
        }
        
        /// <remarks/>
        public string sessionServiceWSDL {
            get {
                return this.sessionServiceWSDLField;
            }
            set {
                this.sessionServiceWSDLField = value;
            }
        }
        
        /// <remarks/>
        public string submissionServiceWSDL {
            get {
                return this.submissionServiceWSDLField;
            }
            set {
                this.submissionServiceWSDLField = value;
            }
        }
        
        /// <remarks/>
        public string queryServiceWSDL {
            get {
                return this.queryServiceWSDLField;
            }
            set {
                this.queryServiceWSDLField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void GetBindingsCompletedEventHandler(object sender, GetBindingsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetBindingsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetBindingsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public BindingResult Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((BindingResult)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void LoginCompletedEventHandler(object sender, LoginCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class LoginCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal LoginCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public LoginResult Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((LoginResult)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void LogoutCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void GetServiceInformationCompletedEventHandler(object sender, GetServiceInformationCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetServiceInformationCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetServiceInformationCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public ServiceInformation Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((ServiceInformation)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    public delegate void GetSessionInformationCompletedEventHandler(object sender, GetSessionInformationCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3752.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetSessionInformationCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetSessionInformationCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public SessionInformation Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((SessionInformation)(this.results[0]));
            }
        }
    }
}

#pragma warning restore 1591