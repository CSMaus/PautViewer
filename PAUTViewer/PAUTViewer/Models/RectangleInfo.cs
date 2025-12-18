using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace PAUTViewer.Models
{
    public class RectangleInfo : INotifyPropertyChanged
    {
        // this is all diaplyed in data grid as table
        // Need to define, what will we use as the 
        private string note = "";
        public string Note
        {
            get { return note; }
            set
            {
                note = value;
                OnPropertyChanged(nameof(Note));
            }
        }

        public string Folder { get; set; }  // take val to shared Folder
        public string File { get; set; }  // take val it to sharedFileName
        public string SpecimenName { get; set; }  // take value from shared var
        public string Company { get; set; }  // common for all defects
        public DateTime DateOfInspection { get; set; }  // common for all defects
        public DateTime DateOfAnalysis { get; set; }  // common for all defects


        public string Channel { get; set; }
        public float DepthMin { get; set; }
        public float DepthMax { get; set; }
        public float ScanIndex1 { get; set; }
        public float ScanIndex2 { get; set; }

        public int UnivIndex { get; set; }

        public float CeneterXAxis1 { get; set; } //Index axis
        public float CeneterXAxis2 { get; set; }

        // new ones, wait for implementation
        public string DefectType { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
