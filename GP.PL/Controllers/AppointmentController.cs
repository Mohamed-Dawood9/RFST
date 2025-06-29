using AutoMapper;
using GP.BLL.Interfaces;
using GP.DAL.Data.Models;
using GP.PL.Helper;
using GP.PL.Services;
using GP.PL.VIewModel;
using GP_BLL.Interfaces;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace GP.PL.Controllers
{
    [AutoValidateAntiforgeryToken]
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISpineProcessingService _spineProcessingService;
        private readonly PdfReportService _pdfReportService;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(IUnitOfWork unitOfWork, IMapper mapper, ISpineProcessingService spineProcessingService,PdfReportService pdfReportService,ILogger<AppointmentController>logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _spineProcessingService = spineProcessingService;
            _pdfReportService = pdfReportService;
            _logger = logger;
        }

        public IActionResult Index(int patientId)
        {
            var patient = _unitOfWork.PatientsRepositry.GetById(patientId);
            if (patient == null)
                return NotFound();

            var appointments = _unitOfWork.AppointmentsRepositry
                   .GetAllWithAnalysis() // Use new method
                   .Where(a => a.PatientId == patientId)
                   .OrderByDescending(a => a.Date)
                   .ToList();

            var mappedApp = _mapper.Map<IEnumerable<Appointment>, IEnumerable<AppointmentViewModel>>(appointments);

            var viewModel = new PatientAppointmentViewModel
            {
                Patient = patient,
                Appointments = mappedApp
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int patientId, IFormFile photo)
        {
            if (photo == null || (!photo.FileName.EndsWith(".jpg") && !photo.FileName.EndsWith(".jpeg")))
                return Json(new { success = false, message = "Please upload a .jpg or .jpeg file." });

            try
            {
                // Step 1: Upload the photo
                string photoPath = DocumentSettings.UpdloadFile(photo, "images");
                if (string.IsNullOrEmpty(photoPath))
                    return Json(new { success = false, message = "Failed to upload the file." });

                // Create Appointment
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst("DoctorId")?.Value ?? "0") : 0,
                    Date = DateTime.Now,
                    OrginalPhotoPath = photoPath
                };

                _unitOfWork.AppointmentsRepositry.Add(appointment);
                int result = _unitOfWork.Complete();
                if (result <= 0)
                    return Json(new { success = false, message = "Failed to save appointment." });

                // Step 2: Create folder structure for 3D processing
                string patientFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "processed_images", $"Patient_{patientId}");
                string appointmentFolder = Path.Combine(patientFolder, $"Appointment_{appointment.Id}");
                Directory.CreateDirectory(appointmentFolder);

                // Step 3: Process the photo for 3D (adapted from .glb processing)
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", photoPath);
                if (!System.IO.File.Exists(imagePath))
                    return Json(new { success = false, message = $"Image file not found at: {imagePath}." });

                // Assuming ProcessSpine can handle .jpg; adjust if a different method is needed
                string stlPath = "E:\\Year 4\\sem2\\gp\\final\\Spine_NIH3D.stl";
                _spineProcessingService.ProcessSpine(imagePath, stlPath, appointmentFolder);

                // Step 4: Get processed HTML files
                string baseFileName = Path.GetFileNameWithoutExtension(photo.FileName);
                var allFiles = Directory.GetFiles(appointmentFolder, "*.html");
                if (allFiles.Length != 3)
                    return Json(new { success = false, message = $"Expected 3 HTML files, found {allFiles.Length}." });

                appointment.ProcessedPhotoPath1 = $"/processed_images/Patient_{patientId}/Appointment_{appointment.Id}/{Path.GetFileName(allFiles[0])}";
                appointment.ProcessedPhotoPath2 = $"/processed_images/Patient_{patientId}/Appointment_{appointment.Id}/{Path.GetFileName(allFiles[1])}";
                appointment.ProcessedPhotoPath3 = $"/processed_images/Patient_{patientId}/Appointment_{appointment.Id}/{Path.GetFileName(allFiles[2])}";

                // Step 5: Read and store the Cobb angle
                var cobbAngleFiles = Directory.GetFiles(appointmentFolder, $"*{baseFileName}_cobb_angle.txt");
                if (cobbAngleFiles.Any())
                {
                    string cobbAngleText = System.IO.File.ReadAllText(cobbAngleFiles.First());
                    appointment.CobbAngle = decimal.TryParse(Regex.Match(cobbAngleText, @"\d+\.\d+").Value, out decimal angle) ? angle : 0m;
                }

                _unitOfWork.AppointmentsRepositry.Update(appointment);
                result = _unitOfWork.Complete();
                if (result <= 0)
                    return Json(new { success = false, message = "Failed to update appointment." });

                // Step 6: Create Analysis for 2D processing
                var analysis = new Analysis
                {
                    PatientId = patientId,
                    DoctorId = appointment.DoctorId,
                    Date = DateTime.Now,
                    OriginalPhotoPath = photoPath,
                    AppointmentId = appointment.Id
                };

                _unitOfWork.AnalysisRepositry.Add(analysis);
                result = _unitOfWork.Complete();
                if (result <= 0)
                    return Json(new { success = false, message = "Failed to save analysis." });

                // Step 7: Create folder structure for 2D processing
                string analysisFolder = Path.Combine(patientFolder, $"Analysis_{analysis.Id}");
                Directory.CreateDirectory(analysisFolder);

                // Step 8: Process the photo for 2D analysis
                var report = await _spineProcessingService.ProcessBackImageAsync(imagePath, analysisFolder);
                string outputImagePath = Path.Combine(analysisFolder, "AnnotatedImage.jpg");
                _spineProcessingService.AnnotateAndSaveImage(report, imagePath, outputImagePath);

                analysis.ProcessedPhotoPath = $"/processed_images/Patient_{patientId}/Analysis_{analysis.Id}/AnnotatedImage.jpg";
                analysis.HDI_S = report.HDI_S;
                analysis.HDI_A = report.HDI_A;
                analysis.HDI_T = report.HDI_T;
                analysis.FAI_C7 = report.FAI_C7;
                analysis.FAI_A = report.FAI_A;
                analysis.FAI_T = report.FAI_T;

                analysis.Keypoints = new List<Keypoint>
                {
                    new Keypoint { AnalysisId = analysis.Id, Name = report.C7.Name, X = report.C7.X, Y = report.C7.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.T7.Name, X = report.T7.X, Y = report.T7.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.LeftHip.Name, X = report.LeftHip.X, Y = report.LeftHip.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.RightHip.Name, X = report.RightHip.X, Y = report.RightHip.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.MidHip.Name, X = report.MidHip.X, Y = report.MidHip.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.LeftScapula.Name, X = report.LeftScapula.X, Y = report.LeftScapula.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.RightScapula.Name, X = report.RightScapula.X, Y = report.RightScapula.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.LeftShoulder.Name, X = report.LeftShoulder.X, Y = report.LeftShoulder.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.RightShoulder.Name, X = report.RightShoulder.X, Y = report.RightShoulder.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.LeftSide.Name, X = report.LeftSide.X, Y = report.LeftSide.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.RightSide.Name, X = report.RightSide.X, Y = report.RightSide.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.LeftUnderArm.Name, X = report.LeftUnderArm.X, Y = report.LeftUnderArm.Y },
                    new Keypoint { AnalysisId = analysis.Id, Name = report.RightUnderArm.Name, X = report.RightUnderArm.X, Y = report.RightUnderArm.Y }
                };

                _unitOfWork.AnalysisRepositry.Update(analysis);
                result = _unitOfWork.Complete();
                if (result <= 0)
                    return Json(new { success = false, message = "Failed to update analysis." });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create error: {ex.Message}");
                return Json(new { success = false, message = $"Processing failed: {ex.Message}" });
            }
        }
        [HttpGet("test-pdf")]
        public IActionResult TestPdf()
        {
            try
            {
                _logger.LogDebug("Starting TestPdf...");
                using var stream = new MemoryStream();
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                document.Add(new Paragraph("Test PDF created successfully."));
                document.Close();
                _logger.LogInformation("Test PDF generated successfully.");
                return File(stream.ToArray(), "application/pdf", "Test.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestPdf Error: {Message}\nInnerException: {InnerMessage}\nType: {ExceptionType}\nHResult: {HResult}",
                    ex.Message, ex.InnerException?.Message, ex.GetType().FullName, ex.HResult);
                return StatusCode(500, new { success = false, message = $"Test PDF failed: {ex.Message} (Inner: {ex.InnerException?.Message})" });
            }
        }

        public async Task<IActionResult> DownloadReport(int? id)
        {
            try
            {
                if (!id.HasValue)
                    return BadRequest(new { success = false, message = "Invalid appointment ID." });

                var appointment = _unitOfWork.AppointmentsRepositry.GetAllWithAnalysis()
                    .Include(a => a.Patient)
                    .Include(a => a.Notes)
                    .FirstOrDefault(a => a.Id == id.Value);
                if (appointment == null)
                    return NotFound(new { success = false, message = "Appointment not found." });

                var analysis = _unitOfWork.AnalysisRepositry.GetAll()
                    .FirstOrDefault(a => a.AppointmentId == id.Value);

                var previousAppointment = _unitOfWork.AppointmentsRepositry.GetAll()
                    .Where(a => a.PatientId == appointment.PatientId && a.Date < appointment.Date)
                    .OrderByDescending(a => a.Date)
                    .FirstOrDefault();

                var previousAnalysis = analysis != null ? _unitOfWork.AnalysisRepositry.GetAll()
                    .Where(a => a.PatientId == appointment.PatientId && a.Date < analysis.Date)
                    .OrderByDescending(a => a.Date)
                    .FirstOrDefault() : null;

                var reportVM = new ReportViewModel
                {
                    PatientId = appointment.PatientId,
                    PatientName = appointment.Patient?.Name ?? "Unknown",
                    Age=appointment.Patient?.Age ?? 0,
                    AppointmentId = appointment.Id,
                    AppointmentDate = appointment.Date,
                    OriginalPhotoPath = appointment.OrginalPhotoPath,
                    ProcessedPhotoPath1 = appointment.ProcessedPhotoPath1,
                    ProcessedPhotoPath2 = appointment.ProcessedPhotoPath2,
                    ProcessedPhotoPath3 = appointment.ProcessedPhotoPath3,
                    CobbAngle = appointment.CobbAngle ?? 0m,
                    Diagnosis = appointment.Diagnosis ?? GetDiagnosis(appointment.CobbAngle),
                    Notes = appointment.Notes.Select(n => new NoteViewModel
                    {
                        Id = n.Id,
                        Content = n.Content,
                        AppointmentId = n.AppointmentId.Value,
                        Source = "Appointment"
                    }).Concat(appointment.Analysis?.Notes.Select(n => new NoteViewModel
                    {
                        Id = n.Id,
                        Content = n.Content,
                        Source = "Analysis"
                    }) ?? Enumerable.Empty<NoteViewModel>()).ToList()
                };

                if (previousAppointment != null)
                {
                    reportVM.PreviousCobbAngle = previousAppointment.CobbAngle;
                    if (previousAppointment.CobbAngle != null && previousAppointment.CobbAngle != 0)
                    {
                        reportVM.CobbProgressPercentage = (double)((previousAppointment.CobbAngle.Value - (appointment.CobbAngle ?? 0m)) / previousAppointment.CobbAngle.Value * 100);
                        reportVM.CobbProgressStatus = reportVM.CobbProgressPercentage >= 0 ? "Improvement" : "Worsening";
                    }
                }

                if (analysis != null)
                {
                    reportVM.AnalysisOriginalPhotoPath = analysis.OriginalPhotoPath;
                    reportVM.AnalysisProcessedPhotoPath = analysis.ProcessedPhotoPath;
                    reportVM.HDI_S = analysis.HDI_S;
                    reportVM.HDI_A = analysis.HDI_A;
                    reportVM.HDI_T = analysis.HDI_T;
                    reportVM.FAI_C7 = analysis.FAI_C7;
                    reportVM.FAI_A = analysis.FAI_A;
                    reportVM.FAI_T = analysis.FAI_T;
                    
                    reportVM.Keypoints = analysis.Keypoints?.Select(k => new KeypointViewModel
                    {
                        Name = k.Name,
                        X = k.X,
                        Y = k.Y
                    }).ToList() ?? new List<KeypointViewModel>();

                    if (previousAnalysis != null)
                    {
                        reportVM.PreviousHDI_S = previousAnalysis.HDI_S;
                        reportVM.PreviousHDI_A = previousAnalysis.HDI_A;
                        reportVM.PreviousHDI_T = previousAnalysis.HDI_T;
                        reportVM.PreviousFAI_C7 = previousAnalysis.FAI_C7;
                        reportVM.PreviousFAI_A = previousAnalysis.FAI_A;
                        reportVM.PreviousFAI_T = previousAnalysis.FAI_T;
                        
                    }
                }

                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                _logger.LogDebug("Calling PdfReportService.GenerateReport for Appointment ID {AppointmentId}", id.Value);
                var pdfBytes = await _pdfReportService.GenerateReport(reportVM, wwwrootPath);
                _logger.LogInformation("PDF generated successfully for Appointment ID {AppointmentId}", id.Value);

                return File(pdfBytes, "application/pdf", $"Report_P{reportVM.PatientId}_A{reportVM.AppointmentId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadReport Error: {Message}\nInnerException: {InnerMessage}\nType: {ExceptionType}\nHResult: {HResult}",
                    ex.Message, ex.InnerException?.Message, ex.GetType().FullName, ex.HResult);
                return StatusCode(500, new { success = false, message = $"Failed to generate PDF: {ex.Message} (Inner: {ex.InnerException?.Message})" });
            }
        }

        private string GetDiagnosis(decimal? cobbAngle)
        {
            if (!cobbAngle.HasValue) return "No Cobb angle provided";
            if (cobbAngle >= 80) return "Very-severe scoliosis";
            if (cobbAngle >= 40) return "Severe scoliosis";
            if (cobbAngle >= 25) return "Moderate scoliosis";
            if (cobbAngle >= 10) return "Mild scoliosis";
            return "No significant scoliosis";
        }
        public IActionResult Details(int? id)
        {
            if (!id.HasValue)
                return BadRequest();
            var appointment = _unitOfWork.AppointmentsRepositry.GetById(id.Value);
            if (appointment == null)
                return NotFound();
            var mappedApp = _mapper.Map<Appointment, AppointmentViewModel>(appointment);
            return View("Details", mappedApp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var appointment = _unitOfWork.AppointmentsRepositry.GetById(id);
            if (appointment == null)
                return Json(new { success = false, message = "Appointment not found." });

            var analysis = _unitOfWork.AnalysisRepositry.GetAll().FirstOrDefault(a => a.AppointmentId == id);
            if (analysis != null)
            {
                _unitOfWork.AnalysisRepositry.Delete(analysis);
            }

            _unitOfWork.AppointmentsRepositry.Delete(appointment);
            _unitOfWork.Complete();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult AddNote(AppointmentViewModel viewModel)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(viewModel.NewNote))
            {
                var appointment = _unitOfWork.AppointmentsRepositry.GetById(viewModel.Id);
                if (appointment == null)
                    return NotFound();

                var note = new Note
                {
                    Content = viewModel.NewNote,
                    AppointmentId = viewModel.Id
                };

                _unitOfWork.NotesRepositry.Add(note);
                _unitOfWork.Complete();
                return RedirectToAction("Details", new { id = viewModel.Id });
            }

            var existingAppointment = _unitOfWork.AppointmentsRepositry.GetById(viewModel.Id);
            if (existingAppointment == null)
                return NotFound();
            var mappedApp = _mapper.Map<Appointment, AppointmentViewModel>(existingAppointment);
            return View("Details", mappedApp);
        }

        [HttpPost]
        public IActionResult DeleteNote(int noteId, int appointmentId)
        {
            var note = _unitOfWork.NotesRepositry.GetById(noteId);
            if (note == null || note.AppointmentId != appointmentId)
                return NotFound();

            _unitOfWork.NotesRepositry.Remove(note);
            _unitOfWork.Complete();
            return RedirectToAction("Details", new { id = appointmentId });
        }
    }
}