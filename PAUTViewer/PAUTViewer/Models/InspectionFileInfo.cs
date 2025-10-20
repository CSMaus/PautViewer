using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace PAUTViewer.Models
{
    public class InspectionFileInfo
    {
        // maybe we need to remove Folder bcs it can vary a lot from vomputer
        // but maybe leave it, bcs it can contain info about specimen and other important
        public string Folder { get; set; }
        public string File { get; set; }
        public string SpecimenName { get; set; }
        public string Company { get; set; }
        public DateTime DateOfInspection { get; set; }  // change to date picker
        public DateTime DateOfAnalysis { get; set; }  // change to date picker

        // do not implement notify property changed
        // will fill rect infos when pressing export

        // to export to the database:
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DBName { get; set; }  // now will use for docker conatiner pattern ONLY!



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
