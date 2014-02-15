using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Common.Interfaces;

namespace Common
{
    public static class Helpers
    {
        public static string GetAPIURL()
        {
#if DEBUG
            string strResp = "http://localhost:49659/9p.svc/1/";
#elif RELEASE
            string strResp = "http://api.nineplaces.com/1/";
#elif RELEASETESTING
           string strResp = "http://testing.nineplaces.com/1/";
#endif
           return strResp;
        }

        public static IApp App
        {
            get;set;

        }

        public static bool IsURLFriendly( string in_strtoCheck )
        {
            Regex RgxUrl = new Regex(@"^[a-zA-Z0-9_@.-]*$");
            return RgxUrl.IsMatch(in_strtoCheck);
        }

        public static string GenerateSlug(string phrase, int maxLength)
        {
            string str = phrase.ToLower();

            // invalid chars, make into spaces
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces/hyphens into one space       
            str = Regex.Replace(str, @"[\s-]+", " ").Trim();
            // cut and trim it
            str = str.Substring(0, str.Length <= maxLength ? str.Length : maxLength).Trim();
            // hyphens
            str = Regex.Replace(str, @"\s", "-");

            return str;
        }

        /// <summary>
        /// Simple helper to determine whether or not a framework element is an ancestor
        /// of another framework element.
        /// </summary>
        /// <param name="in_oAncestor"></param>
        /// <param name="in_oDescendant"></param>
        /// <returns></returns>
        public static bool IsAncestorControl(FrameworkElement in_oAncestor, FrameworkElement in_oDescendant)
        {
            FrameworkElement curParent = in_oDescendant;
            while (curParent != null && curParent != in_oAncestor)
            {
                curParent = System.Windows.Media.VisualTreeHelper.GetParent(curParent) as FrameworkElement;
            }

            return curParent == in_oAncestor;
        }

        public static bool TryAddValue<TKey, TValue>(this Dictionary<TKey, TValue> in_dict, TKey Key, TValue Value)
        {
            if (!in_dict.ContainsKey(Key))
                in_dict.Add(Key,Value);
            else
                in_dict[Key] = Value;
            return true;
        }

        public static string URLFriendly(this string phrase)
        {
            string str = Regex.Replace(phrase, @"[^a-z0-9\s-]", ""); // invalid chars           
            str = Regex.Replace(str, @"\s+", " ").Trim(); // convert multiple spaces into one space   
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim(); // cut and trim it   
            str = Regex.Replace(str, @"\s", "-"); // hyphens   

            return str;
        }


        public static void CenterPopup(this System.Windows.Controls.Primitives.Popup in_pToCenter)
        {
            in_pToCenter.Child.UpdateLayout();

            double dWidth = Application.Current.Host.Content.ActualWidth;
            double dHeight = Application.Current.Host.Content.ActualHeight;

            FrameworkElement fe = in_pToCenter.Child as FrameworkElement;

            in_pToCenter.VerticalOffset = (dHeight / 2.0) - (fe.DesiredSize.Height / 2.0);
            in_pToCenter.HorizontalOffset = (dWidth / 2.0) - (fe.DesiredSize.Width / 2.0);
        }

    }

    
}
