using GreatPush.ReactNative;
using ReactNative;
using ReactNative.Modules.Core;
using ReactNative.Shell;
using System.Collections.Generic;

namespace GreatPushDemoApp
{
    class MainPage : ReactPage
    {

        public override string MainComponentName
        {
            get
            {
                return "GreatPushDemoApp";
            }
        }

        private GreatPushReactPackage greatPushReactPackage = null;
        public override string JavaScriptBundleFile
        {
            get
            {

                greatPushReactPackage = new GreatPushReactPackage("deployment-key-here", this);

#if BUNDLE
                return greatPushReactPackage.GetJavaScriptBundleFile();
#else
                return null;
#endif
            }
        }


        public override List<IReactPackage> Packages
        {
            get
            {
                return new List<IReactPackage>
                {
                    new MainReactPackage(),
                    greatPushReactPackage
                };
            }
        }

        public override bool UseDeveloperSupport
        {
            get
            {
#if !BUNDLE || DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }

}
