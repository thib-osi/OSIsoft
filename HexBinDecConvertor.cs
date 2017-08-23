// Copyright (C) 2008-2010 OSIsoft, LLC. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED AS IS WITHOUT WARRANTY OF ANY KIND,
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE OR NONINFRINGEMENT.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.UnitsOfMeasure;
using OSIsoft.AF.Time;

namespace OSIsoft.AF.Asset.DataReference
{
    // Implementation of the data reference
    [Serializable]
    [Guid("52a07485-3953-4166-bff3-7b7b341d51aa")]
    [Description("HexBinDec Convertor;Can convert attribute from Hex, Dec or Bin to Hex, Dec or Bin")]
    public class HexBinDecConvertor : AFDataReference
    {
        private string configString = String.Empty;

        public HexBinDecConvertor()
            : base()
        {
        }

        #region Implementation of AFDataReference
        public override AFDataReferenceContext SupportedContexts
        {
            get
            {
                return (AFDataReferenceContext.All);
            }
        }

        public override string ConfigString
        {
            // The ConfigString property is used to store and load the configuration of this data reference.
            get
            {
                return configString;
            }
            set
            {
                if (ConfigString != value)
                {
                    if (value != null)
                        configString = value.Trim();

                    // notify SDK and clients of change.  Required to have changes saved.
                    SaveConfigChanges();

                    CheckDataType();
                }
            }
        }

        public override AFAttributeList GetInputs(object context)
        {
            // Loop through the config string, looking for attributes
            // The Config string is semicolon separated list of attributes and strings
            // Strings must be enclosed in " "
            // Will also handle standard AF substitions (%ELEMENT%, %TIME%, etc.)
            AFAttributeList paramAttributes = null;
            
            string[] subStrings = ConfigString.Split(';');

            string s = subStrings[0].Trim();
            String subst = SubstituteParameters(s, this, context, null);

            if (!String.IsNullOrEmpty(subst))
            {
                // Get attribute will resolve attribute references 
                AFAttribute attr = GetAttribute(subst);
                if (attr == null || attr.IsDeleted)
                {
                    throw new ApplicationException(String.Format("Unknown attribute '{0}'", s));
                }
                if (paramAttributes == null)
                    paramAttributes = new AFAttributeList();
                paramAttributes.Add(attr);
            }

            return paramAttributes;
        }

        public override AFValue GetValue(object context, object timeContext, AFAttributeList inputAttributes, AFValues inputValues)
        {
            // Evaluate
            AFTime timestamp = AFTime.MinValue;
            if (configString == null) configString = String.Empty;

            //StringBuilder sb = new StringBuilder();
            string sValue = "No Data";
            int decValue;
            string hexValue;
            int binValue;
            //string[] sValue;
            //sValue = new string[] { "N/A" };

            string[] subStrings = configString.Split(';');
            try
            {


            if (subStrings.Length > 2)
                return new AFValue("Too many arguments in the Config String property", timestamp, null, AFValueStatus.Bad);
            else
            {
                if (subStrings[0].StartsWith("\""))
                {
                    return new AFValue("First argument must be a valid AF attribute", timestamp, null, AFValueStatus.Bad);
                }
                else if (subStrings[0] != String.Empty)
                {
                    if (inputValues != null)
                    {
                        if (subStrings[1] != String.Empty) {
                            if (Int32.Parse(subStrings[1]) == 1) {
                                // 1= Conversion from Hex to Bin
                                binValue = Convert.ToInt32(subStrings[0], 2);
                                sValue = binValue.ToString("X"); }
                            else if (Int32.Parse(subStrings[1]) == 2) {
                                // 2= Conversion from Hex to Dec
                                decValue = Convert.ToInt32(subStrings[0], 16);
                                sValue = decValue.ToString("X");
                                    }
                            else if (Int32.Parse(subStrings[1]) == 3) {
                                // 3= Conversion from Bin to Hex
                                ; }
                            else if (Int32.Parse(subStrings[1]) == 4) {
                                // 4= Conversion from Bin to Dec
                                ; }
                            else if (Int32.Parse(subStrings[1]) == 5) {
                                // 5= Conversion from Dec to Hex
                                hexValue = subStrings[0].ToString("X");
                                sValue = hexValue;
                            }
                            else if (Int32.Parse(subStrings[1]) == 6) {
                                // 6= Conversion from Dec to Bin
                                ; }
                            else
                                return new AFValue("Missing/Wrong argument for the convertion type (integer from 1 to 6)", timestamp, null, AFValueStatus.Bad);
                                }
                                /* if (myStringArray.Length > myArrayIndex)
                                        sValue = myStringArray[myArrayIndex];
                                else 
                                    return new AFValue("N/A", timestamp, null, AFValueStatus.Good);*/
                                else
                                    return new AFValue("Missing/Wrong argument for the convertion type (integer from 1 to 6)", timestamp, null, AFValueStatus.Bad);

                        //sValue = myStringArray[0];
                    }
                    else
                        return new AFValue("Invalid data sent to GetValue", timestamp, null, AFValueStatus.Bad);
                    
                }
            }

            // should be returning effective date as absolute minimum
            if (timestamp.IsEmpty && Attribute != null)
            {
                if (Attribute.Element is IAFVersionable)
                    timestamp = ((IAFVersionable)Attribute.Element).Version.EffectiveDate;
                else if (Attribute.Element is AFEventFrame)
                    timestamp = ((AFEventFrame)Attribute.Element).StartTime;
            }
            else if (timestamp.IsEmpty && timeContext is AFTime)
                timestamp = (AFTime)timeContext;

            return new AFValue(sValue, timestamp);


            }
            catch (Exception e)
            {
                return new AFValue(string.Format("An error occurred: ",e.Message), timestamp);
                throw;
            }

        }
        #endregion

        // Since base property 'IsInitializing' only exists in AF 2.1 or later, must
        //  separate the call into the following two methods because an exception is
        //  thrown when 'BaseIsInitializing' is compiled by the CLR.
        //  This would only occur when a AF 2.0 client connects to an AFServer 2.1.
        private bool CheckIsInitializing()
        {
            try
            {
                return BaseIsInitializing();
            }
            catch { }
            return false;
        }
        private bool BaseIsInitializing()
        {
            return IsInitializing;
        }

        internal void CheckDataType()
        {
            if (CheckIsInitializing()) return;
            if (Attribute != null && Attribute.Template != null) return; // can't do anything
            // check to see we are already dirty
            if (Attribute != null && Attribute.Element is IAFTransactable && !((IAFTransactable)Attribute.Element).IsDirty) return;
            if (Template != null && !Template.ElementTemplate.IsDirty) return;

            Type type = null;
            if (Attribute != null)
                type = Attribute.Type;
            else if (Template != null)
                type = Template.Type;

            if (type != typeof(string))
            {
                if (Attribute != null)
                    Attribute.Type = typeof(String);
                else if (Template != null)
                    Template.Type = typeof(String);
            }
        }
    }
}
