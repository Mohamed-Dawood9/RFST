using System.Collections.Generic;

namespace GP.PL.VIewModel
{
    public class ReportViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public int Age { get; set; }
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }

        // 3D Spine Analysis (from AppointmentViewModel)
        public string OriginalPhotoPath { get; set; } // Original spine image
        public string ProcessedPhotoPath1 { get; set; } // Preprocessed 3D model
        public string ProcessedPhotoPath2 { get; set; } // Heatmap
        public string ProcessedPhotoPath3 { get; set; } // Fitted spine
        public decimal? CobbAngle { get; set; }
        public string Diagnosis { get; set; }
        public decimal? PreviousCobbAngle { get; set; }
        public double? CobbProgressPercentage { get; set; }
        public string CobbProgressStatus { get; set; }

        // 2D Posture Analysis (from AnalysisViewModel)
        public string AnalysisOriginalPhotoPath { get; set; } // Original back image
        public string AnalysisProcessedPhotoPath { get; set; } // Annotated back image
        public float HDI_S { get; set; }
        public float HDI_A { get; set; }
        public float HDI_T { get; set; }
        public float FAI_C7 { get; set; }
        public float FAI_A { get; set; }
        public float FAI_T { get; set; }
       
        public float? PreviousHDI_S { get; set; }
        public float? PreviousHDI_A { get; set; }
        public float? PreviousHDI_T { get; set; }
        public float? PreviousFAI_C7 { get; set; }
        public float? PreviousFAI_A { get; set; }
        public float? PreviousFAI_T { get; set; }
        
        public string POTSIProgressStatus { get; set; }
        public List<KeypointViewModel> Keypoints { get; set; } = new List<KeypointViewModel>();

        // Notes (from Appointment only)
        public List<NoteViewModel> Notes { get; set; } = new List<NoteViewModel>();
    }

    public class KeypointViewModel
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }
}