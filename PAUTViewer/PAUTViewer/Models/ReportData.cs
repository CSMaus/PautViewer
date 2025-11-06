using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace PAUTViewer.Models
{
    public class ReportData : INotifyPropertyChanged
    {
        #region Header Information
        private DateTime _reportDate { get; set; } = DateTime.Today; // todo: convert to the format like: 12 MAY 2025
        public DateTime ReportDate
        {
            get => _reportDate;
            set
            {
                if (_reportDate != value)
                {
                    _reportDate = value;
                    OnPropertyChanged(nameof(ReportDate));
                }
            }
        }

        private string _workOrderNumber { get; set; } = "W30430881 (2EA)";
        public string WorkOrderNumber
        {
            get { return _workOrderNumber; }
            set
            {
                if (_workOrderNumber != value)
                {
                    _workOrderNumber = value;
                    OnPropertyChanged(nameof(WorkOrderNumber));
                }
            }
        }

        private string _plant { get; set; } = "KAL";
        public string Plant
        {
            get => _plant;
            set
            {
                if (_plant != value)
                {
                    _plant = value;
                    OnPropertyChanged(nameof(Plant));
                }
            }
        }

        // todo: remove it
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 3;  // todo: remove it

        public string LogoPath { get; set; }
        #endregion

        #region Component Identification
        private string _programName { get; set; } = "A350XWB_CARGODOOR";
        public string ProgramName
        {
            get => _programName;
            set
            {
                if (_programName != value)
                {
                    _programName = value;
                    OnPropertyChanged(nameof(ProgramName));
                }
            }
        }

        private string _partNumber = "L3P-CD-1465";
        public string PartNumber
        {
            get => _partNumber;
            set
            {
                if (_partNumber != value)
                {
                    _partNumber = value;
                    OnPropertyChanged(nameof(PartNumber));
                }
            }
        }

        private string _componentDescription = "EDGE FRAME FWD";
        public string ComponentDescription
        {
            get => _componentDescription;
            set
            {
                if (_componentDescription != value)
                {
                    _componentDescription = value;
                    OnPropertyChanged(nameof(ComponentDescription));
                }
            }
        }

        private string _serialNumber = "K9998";
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (_serialNumber != value)
                {
                    _serialNumber = value;
                    OnPropertyChanged(nameof(SerialNumber));
                }
            }
        }
        #endregion

        #region Test Requirements

        public string InspectionProcedureNumber { get; set; } = "350U108 B";

        private string _relatedDocuments = "AITM6-0011, AITM6-0012";
        public string RelatedDocuments
        {
            get => _relatedDocuments;
            set
            {
                if (_relatedDocuments != value)
                {
                    _relatedDocuments = value;
                    OnPropertyChanged(nameof(RelatedDocuments));
                }
            }
        }

        #endregion

        #region Non Destructive Inspection

        private string _inspectionMethod = "ULTRASONIC TESTING";
        public string InspectionMethod
        {
            get => _inspectionMethod;
            set
            {
                if (_inspectionMethod != value)
                {
                    _inspectionMethod = value;
                    OnPropertyChanged(nameof(InspectionMethod));
                }
            }
        }

        private string _inspectionTechnique = "AUPE / MUPE";
        public string InspectionTechnique
        {
            get => _inspectionTechnique;
            set
            {
                if (_inspectionTechnique != value)
                {
                    _inspectionTechnique = value;
                    OnPropertyChanged(nameof(InspectionTechnique));
                }
            }
        }

        private string _inspectionClass = "I";
        public string InspectionClass
        {
            get => _inspectionClass;
            set
            {
                if (_inspectionClass != value)
                {
                    _inspectionClass = value;
                    OnPropertyChanged(nameof(InspectionClass));
                }
            }
        }

        private string _inspectionCategory = "N/A";
        public string InspectionCategory
        {
            get => _inspectionCategory;
            set
            {
                if (_inspectionCategory != value)
                {
                    _inspectionCategory = value;
                    OnPropertyChanged(nameof(InspectionCategory));
                }
            }
        }

        private string _equipment = "MATRIXEYE EX / #1 MUE";
        public string Equipment
        {
            get => _equipment;
            set
            {
                if (_equipment != value)
                {
                    _equipment = value;
                    OnPropertyChanged(nameof(Equipment));
                }
            }
        }

        private string _acquisitionFileName = "L3P-CD-1465_W30430881";
        public string AcquisitionFileName
        {
            get => _acquisitionFileName;
            set
            {
                if (_acquisitionFileName != value)
                {
                    _acquisitionFileName = value;
                    OnPropertyChanged(nameof(AcquisitionFileName));
                }
            }
        }

        private string _parametersReference = "REFER TO TECHNIQUE SHEET 350U108 B 7. [2] SETUP";
        public string ParametersReference
        {
            get => _parametersReference;
            set
            {
                if (_parametersReference != value)
                {
                    _parametersReference = value;
                    OnPropertyChanged(nameof(ParametersReference));
                }
            }
        }

        #endregion

        #region Calibration
        /// <summary>
        /// Reference standard number (e.g., "RS-II-KAL-CL-301-0/-302-0/-303-0")
        /// </summary>
        public string ReferenceStandardNumber { get; set; } = "RS-II-KAL-CL-301-0/-302-0/-303-0";
        #endregion

        #region Inspection Results
        public ObservableCollection<InspectionResultEntry> InspectionResults { get; set; } = new ObservableCollection<InspectionResultEntry>();

        private int _selectedInspResIdx;
        public int SelectedInspResIdx
        {
            get { return _selectedInspResIdx; }
            set
            {
                _selectedInspResIdx = value;
                OnPropertyChanged(nameof(SelectedInspResIdx));
            }
        }
        #endregion


        #region Report Information
        private string _inspectionReportId = "INSPECTION REPORT AITM6-0011";
        public string InspectionReportId
        {
            get => _inspectionReportId;
            set
            {
                if (_inspectionReportId != value)
                {
                    _inspectionReportId = value;
                    OnPropertyChanged(nameof(InspectionReportId));
                }
            }
        }

        private string _revisionInfo = "7 JUN, 2011";   // "Rev. (A) 7 JUN, 2011";
        public string RevisionInfo
        {
            get => _revisionInfo;
            set
            {
                if (_revisionInfo != value)
                {
                    _revisionInfo = value;
                    OnPropertyChanged(nameof(RevisionInfo));
                }
            }
        }
        #endregion

        #region Constructor
        public ReportData()
        {
            InspectionResults = new ObservableCollection<InspectionResultEntry>
            {
                new InspectionResultEntry
                {
                    AreaToBeInspected = "(2EA)-Web",
                    NdtTestNumber = "L3P-CD-1465_W30430881",
                    Results = "Accept",
                    Inspector = "LEE JAE OG",
                    InspectionDate = "12 MAY 2025",
                    StampComments = "U28"
                },
                new InspectionResultEntry
                {
                    AreaToBeInspected = "(2EA)-Flange",
                    NdtTestNumber = "L3P-CD-1465_W30430881",
                    Results = "Accept",
                    Inspector = "LEE JAE OG",
                    InspectionDate = "12 MAY 2025",
                    StampComments = "U28"
                },
                new InspectionResultEntry
                {
                    AreaToBeInspected = "(2EA)-Corner",
                    NdtTestNumber = "L3P-CD-1465_W30430881",
                    Results = "Accept",
                    Inspector = "LEE JAE OG",
                    InspectionDate = "12 MAY 2025",
                    StampComments = "U28"
                }
            };
            //Defects = new ObservableCollection<DefectData>();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the formatted related documents string
        /// </summary>
        /// <returns>Formatted string of related documents</returns>
        public string GetRelatedDocumentsString()
        {
            return RelatedDocuments != null ? RelatedDocuments : string.Empty;
        }

        /// <summary>
        /// Gets the page information string (e.g., "Pages 1 / 3")
        /// </summary>
        /// <returns>Formatted page information</returns>
        public string GetPageInfo()
        {
            return $"Pages {CurrentPage} / {TotalPages}";
        }

        /// <summary>
        /// Validates that all required fields are populated
        /// </summary>
        /// <returns>True if all required fields have values</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(WorkOrderNumber) &&
                   !string.IsNullOrWhiteSpace(Plant) &&
                   !string.IsNullOrWhiteSpace(PartNumber) &&
                   !string.IsNullOrWhiteSpace(SerialNumber) &&
                   !string.IsNullOrWhiteSpace(InspectionProcedureNumber);
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Represents a single inspection result entry in the inspection results table
    /// </summary>
    public class InspectionResultEntry : INotifyPropertyChanged
    {
        private string _areaToBeInspected;
        public string AreaToBeInspected
        {
            get => _areaToBeInspected;
            set
            {
                if (_areaToBeInspected != value)
                {
                    _areaToBeInspected = value;
                    OnPropertyChanged(nameof(AreaToBeInspected));
                }
            }
        }

        private string _ndtTestNumber;
        public string NdtTestNumber
        {
            get => _ndtTestNumber;
            set
            {
                if (_ndtTestNumber != value)
                {
                    _ndtTestNumber = value;
                    OnPropertyChanged(nameof(NdtTestNumber));
                }
            }
        }

        private string _results;
        public string Results
        {
            get => _results;
            set
            {
                if (_results != value)
                {
                    _results = value;
                    OnPropertyChanged(nameof(Results));
                }
            }
        }

        private string _inspector;
        public string Inspector
        {
            get => _inspector;
            set
            {
                if (_inspector != value)
                {
                    _inspector = value;
                    OnPropertyChanged(nameof(Inspector));
                }
            }
        }

        private string _inspectionDate;
        public string InspectionDate
        {
            get => _inspectionDate;
            set
            {
                if (_inspectionDate != value)
                {
                    _inspectionDate = value;
                    OnPropertyChanged(nameof(InspectionDate));
                }
            }
        }

        private string _stampComments;
        public string StampComments
        {
            get => _stampComments;
            set
            {
                if (_stampComments != value)
                {
                    _stampComments = value;
                    OnPropertyChanged(nameof(StampComments));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
