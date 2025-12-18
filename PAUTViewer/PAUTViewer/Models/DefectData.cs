using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace PAUTViewer.Models
{
    /// <summary>
    /// Data model for storing defect information used in the defects table (Page 2 of the report)
    /// </summary>
    public class DefectData : INotifyPropertyChanged
    {
        #region Private Fields
        private string _defectNumber;
        private string _typeOfDefect;
        private string _inspectionMethod;
        private string _attenuation;
        private float? _thicknessOfPart;
        private string _depthFrom;
        private string _size;
        private string _pictureNumber;
        private string _pictureNumber2;
        private string _locatedIn_PartName;  // Part name ("Weld 1", "Plate B", "Web flange")
        private string _locatedIn_Zone;  // Zone ( "Root" if shallow; "Cap" if near top; "HAZ" if near fusion line)
        private string _position_fromDatum;  // Distance from datum (scan axis position of defect start) / Beam index or X
        private string _position_beamAxis;  // Distance from datum (scan axis position of defect start) / Beam index or X
        private string _remark;
        #endregion

        #region Public Properties
        /// <summary>
        /// Defect identification number (e.g., "001", "002", etc.)
        /// </summary>
        public string DefectNumber
        {
            get => _defectNumber;
            set
            {
                if (_defectNumber != value)
                {
                    _defectNumber = value;
                    OnPropertyChanged(nameof(DefectNumber));
                }
            }
        }

        /// <summary>
        /// Type of defect detected (e.g., "Crack", "Void", "Delamination", "Porosity", "Inclusion")
        /// </summary>
        public string TypeOfDefect
        {
            get => _typeOfDefect;
            set
            {
                if (_typeOfDefect != value)
                {
                    _typeOfDefect = value;
                    OnPropertyChanged(nameof(TypeOfDefect));
                }
            }
        }

        /// <summary>
        /// Inspection method used to detect the defect (e.g., "UT", "PAUT", "Conventional UT")
        /// </summary>
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

        /// <summary>
        /// Attenuation value in dB (nullable for cases where not applicable)
        /// </summary>
        public string Attenuation
        {
            get => _attenuation;
            set
            {
                if (_attenuation != value)
                {
                    _attenuation = value;
                    OnPropertyChanged(nameof(Attenuation));
                    OnPropertyChanged(nameof(AttenuationString));
                }
            }
        }

        /// <summary>
        /// String representation of attenuation for display purposes
        /// </summary>
        public string AttenuationString => Attenuation ?? string.Empty; // (ToString("F1"))  ?? means != null? value itself

        /// <summary>
        /// Thickness of the part at the defect location in mm (nullable)
        /// </summary>
        public float? ThicknessOfPart
        {
            get => _thicknessOfPart;
            set
            {
                if (_thicknessOfPart != value)
                {
                    _thicknessOfPart = value;
                    OnPropertyChanged(nameof(ThicknessOfPart));
                    OnPropertyChanged(nameof(ThicknessString));
                }
            }
        }

        /// <summary>
        /// String representation of thickness for display purposes
        /// </summary>
        public string ThicknessString => ThicknessOfPart?.ToString("F1") ?? string.Empty;

        /// <summary>
        /// Depth information or "from" reference (e.g., "2.5 from surface", "mid-thickness")
        /// </summary>
        public string DepthFrom
        {
            get => _depthFrom;
            set
            {
                if (_depthFrom != value)
                {
                    _depthFrom = value;
                    OnPropertyChanged(nameof(DepthFrom));
                }
            }
        }

        /// <summary>
        /// Size of the defect (e.g., "5.2 x 2.1", "Ø 3.5", "Length: 12.5")
        /// </summary>
        public string Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }

        /// <summary>
        /// Reference to picture/image number showing the defect
        /// </summary>
        public string PictureNumber
        {
            get => _pictureNumber;
            set
            {
                if (_pictureNumber != value)
                {
                    _pictureNumber = value;
                    OnPropertyChanged(nameof(PictureNumber));
                }
            }
        }
        public string PictureNumber2
        {
            get => _pictureNumber2;
            set
            {
                if (_pictureNumber2 != value)
                {
                    _pictureNumber2 = value;
                    OnPropertyChanged(nameof(PictureNumber2));
                }
            }
        }

        /// <summary>
        /// Location description where the defect is found (e.g., "Web", "Flange", "Corner radius")
        /// </summary>
        public string LocatedIn_PartName
        {
            get => _locatedIn_PartName;
            set
            {
                if (_locatedIn_PartName != value)
                {
                    _locatedIn_PartName = value;
                    OnPropertyChanged(nameof(LocatedIn_PartName));
                }
            }
        }
        public string LocatedIn_Zone
        {
            get => _locatedIn_Zone;
            set
            {
                if (_locatedIn_Zone != value)
                {
                    _locatedIn_Zone = value;
                    OnPropertyChanged(nameof(LocatedIn_Zone));
                }
            }
        }

        /// <summary>
        /// Position coordinates or description (e.g., "X: 125, Y: 67", "Station 15-20")
        /// </summary>
        public string Position_fromDatum
        {
            get => _position_fromDatum;
            set
            {
                if (_position_fromDatum != value)
                {
                    _position_fromDatum = value;
                    OnPropertyChanged(nameof(Position_fromDatum));
                }
            }
        }
        public string Position_beamAxis
        {
            get => _position_beamAxis;
            set
            {
                if (_position_beamAxis != value)
                {
                    _position_beamAxis = value;
                    OnPropertyChanged(nameof(Position_beamAxis));
                }
            }
        }

        /// <summary>
        /// Additional remarks or comments about the defect
        /// </summary>
        public string Remark
        {
            get => _remark;
            set
            {
                if (_remark != value)
                {
                    _remark = value;
                    OnPropertyChanged(nameof(Remark));
                }
            }
        }
        #endregion


        #region Validation Methods
        /// <summary>
        /// Validates that the defect data has minimum required information
        /// </summary>
        /// <returns>True if defect has required fields populated</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DefectNumber) &&
                   !string.IsNullOrWhiteSpace(TypeOfDefect) &&
                   !string.IsNullOrWhiteSpace(InspectionMethod);
        }

        /// <summary>
        /// Gets a summary description of the defect
        /// </summary>
        /// <returns>Summary string</returns>
        public string GetSummary()
        {
            return $"Defect #{DefectNumber}: {TypeOfDefect} detected by {InspectionMethod}";
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// Creates a sample defect for testing purposes
        /// </summary>
        /// <param name="defectNumber">Defect number</param>
        /// <returns>Sample defect data</returns>
        public static DefectData CreateSampleDefect(string defectNumber)
        {
            return new DefectData
            {
                DefectNumber = defectNumber,
                TypeOfDefect = "Delamination",
                InspectionMethod = "PAUT",
                Attenuation = "6.2",
                ThicknessOfPart = 12.5f,
                DepthFrom = "2.1 from surface",
                Size = "8.3 x 1.2",
                PictureNumber = $"IMG_{defectNumber}",
                PictureNumber2 = $"IMG_{defectNumber}",
                LocatedIn_PartName = "Web section",  // Flange, Web Flange, Web Section, 
                LocatedIn_Zone = "Root",  // Root, Cap (near top), HAZ  (near fusion line)
                Position_fromDatum = "120 mm",
                Position_beamAxis = "X: 25 - 35 mm",
                Remark = "Requires repair"
            };
        }

        /// <summary>
        /// Creates an empty defect entry for data input
        /// </summary>
        /// <param name="defectNumber">Defect number</param>
        /// <returns>Empty defect data with only number set</returns>
        public static DefectData CreateEmptyDefect(string defectNumber)
        {
            return new DefectData
            {
                DefectNumber = defectNumber
            };
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Override Methods
        public override string ToString()
        {
            return GetSummary();
        }

        public override bool Equals(object obj)
        {
            if (obj is DefectData other)
            {
                return DefectNumber == other.DefectNumber;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return DefectNumber?.GetHashCode() ?? 0;
        }
        #endregion
    }

    public enum DefectType
    {
        Unknown,
        FO,
        Crack,
        Void,
        Delamination,
        Porosity,
        Inclusion,
        Disbond,
        Corrosion,
        WeldDefect,
        Other
    }
    public enum InspectionMethodType
    {
        PAUT,
        ConventionalUT,
        ToFD,
        EddyCurrent,
        Radiography,
        Other,
    }
}
