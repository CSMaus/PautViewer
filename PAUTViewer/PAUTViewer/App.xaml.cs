using ControlzEx.Theming;
using SciChart.Charting.Visuals;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PAUTViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // expired due to the trial period
            // SciChartSurface.SetRuntimeLicenseKey("9UrhJfQsZgFaYAeTYHAe7dmhBo4R+3g/3jchBsW8QaKZ1JHncXEYUnlk3keOjt+pKZHum9hJVIcseXTDQKYOJB+KafapuvFMtsNfzZwG99QhL5tdA3OnjO7Lw724vuRoy7SAxhbCOKVfQnfvVv+DKKCia62yvpBEq4O2tIe+bD78Ne3HeWB9dLyNHv9j0TAD9lhspW3bYKP9GnF75V8krbK1+oFfgPGdt7rxkK2vyvuaHIqXFG/j+T4p+Zcz+xbMjmeBWc9GsS3qawZogR8Mghys5bnNXZBHoERO5Puv5krR/M3sIgdhAlGLu/0Dttgg42kTs0jwDJnyQukt891NOSZ6c/TtrYnsGH65nvNkJpdkjjvFQviiYr79F/597MVFT1TFcj/HS/UJHRd6hqUHX+xlGmU1giF1nHFuNr/HkNKAdKU1IP1+RCGCpXovntrE/23FH563Gk3v2sp2ALLo7//P87GzP663x+lDxyMc/aqXK8LXwbFgOdIbKg7Aci7Ywnz5kdUlk4zSOGHEgmWEFgrEaOPYFcIvHgFvt9ByeHJhlywnsE6Qc3XhbICVq1vsL4mlFfqaww==");
            SciChartSurface.SetRuntimeLicenseKey("M4Pdq5OCg9BNaacI1r+4mKgvWB2ls7NefVy9FWABEZ7BY+R2uwJioNScVulqlE1HX613DMyt+IrjnZb53GtLsawsmBJvlVLQZi7KI+cTbklpX/f+D5yuEqyPirtiFsbMYkwZKeqz3p8sLqj6FGYAnJcV3u58oIoKVHzgKmoSSafOC28zfV4j0VrPChlYO2lUUdiJ6+ypb1MaTFy1W+IWvkV/COxGNpffqAtAGf2tt4JI7dIrz15YCFYuBdOrzMUDr2SwH+lNemmxpVidZUGvcC5TywzWn6omzySylOb4EjWGH7z4slvy3y84RurkYnF8TjC2ogTV06zqVOaYQpfSa7roMJ8jn4L8qp3J+5AlGMsik3cBgpPdmrDSfAP/BbVPtNrLVMxwMjSxwVCV6+NwZ61ZLrmCURvBLLqe3kvB1g7SVf3iuL+U9TnggCJypjKvMZlT+vEibgb7aAPIGU8ku6y1wuYEvexVAlfEPlKV0GTtyeClwUYSXcjJMy4BnCguYvfrMgFG1UOqPx3AQNfRw6r5NeZFCiFS");

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // base themes: "Light", "Dark"
            // color schemes: "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
            // "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber",
            // "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
            // Set the application theme like this: Dark.Green
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
        }
    }

}
