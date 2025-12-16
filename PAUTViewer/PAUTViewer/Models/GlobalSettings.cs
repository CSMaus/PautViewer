using System;
using System.Collections.Generic;
using System.Text;

namespace PAUTViewer.Models
{
    public static class GlobalSettings
    {
        public static bool IsTurnOffNotifications { get; private set; }
        public static void SetIsTurnOffNotifications(bool state) => IsTurnOffNotifications = state;

        public static string Language { get; private set; }
        public static void SetLanguage(string lang) => Language = lang;
    }
}
