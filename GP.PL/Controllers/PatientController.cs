using AutoMapper;
using GP.BLL.Interfaces;
using GP.DAL.Data.Models;
using GP.PL.VIewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GP.PL.Controllers
{
    [AutoValidateAntiforgeryToken]
    [Authorize]
    public class PatientController : Controller
    {
       
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PatientController(IUnitOfWork unitOfWork,IMapper mapper)
        {
         
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public IActionResult Index(string searchInp)
        {
            var patients = Enumerable.Empty<Patient>();

            if (string.IsNullOrEmpty(searchInp))
                patients = _unitOfWork.PatientsRepositry.GetAll();
            else
                patients = _unitOfWork.PatientsRepositry.GetEmployeeByName(searchInp.ToLower());

            var mappedpatient = _mapper.Map<IEnumerable<Patient>, IEnumerable<PatientViewModel>>(patients);
            return View(mappedpatient);
        }
        public IActionResult Create()
        {
         
            return View();
        }
        [HttpPost]
        public IActionResult Create(PatientViewModel paitentVM)
        {

            if (ModelState.IsValid)
            {

                var mappedPatient = _mapper.Map<PatientViewModel, Patient>(paitentVM);
                _unitOfWork.PatientsRepositry.Add(mappedPatient);
                var count = _unitOfWork.Complete();
                if (count > 0)
                    return RedirectToAction(nameof(Index));

            }
            return View(paitentVM);
        }
        public IActionResult Details(int? id, string viewName = "Details")
        {
            if (!id.HasValue)
                return BadRequest();
            var patient = _unitOfWork.PatientsRepositry.GetById(id.Value);
            var mappedPatient = _mapper.Map<Patient, PatientViewModel>(patient);
            if (patient == null)
                return NotFound();
            return View(viewName, mappedPatient);
        }
        public IActionResult Edit(int? id)
        {
            return Details(id, "Edit");
        }
        [HttpPost]
        public IActionResult Edit([FromRoute] int id, PatientViewModel patientVm)
        {

            if (id != patientVm.Id)
                return BadRequest();
            if (ModelState.IsValid)
            {
                try
                {
                    var mappedDept = _mapper.Map<PatientViewModel, Patient>(patientVm);
                    _unitOfWork.PatientsRepositry.Update(mappedDept);
                    _unitOfWork.Complete();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            return View(patientVm);

        }
        public IActionResult Delete(int? id)
        {
            return Details(id, "Delete");
        }
        
        [HttpPost]
        public IActionResult Delete([FromRoute] int id, PatientViewModel patientVm)
        {
            if (id != patientVm.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the full patient entity with related appointments
                    var patient = _unitOfWork.PatientsRepositry.GetPatientWithAppointmentsAndNotes(id); // You must implement this

                    if (patient == null)
                        return NotFound();

                    foreach (var appointment in patient.Appointments)
                    {
                        // Delete Notes linked to the appointment
                        var appointmentNotes = appointment.Notes?.ToList();
                        if (appointmentNotes != null && appointmentNotes.Any())
                            _unitOfWork.NotesRepositry.RemoveRange(appointmentNotes);

                        // Delete Analysis and its notes
                        if (appointment.Analysis != null)
                        {
                            var analysisNotes = appointment.Analysis.Notes?.ToList();
                            if (analysisNotes != null && analysisNotes.Any())
                                _unitOfWork.NotesRepositry.RemoveRange(analysisNotes);

                            _unitOfWork.AnalysisRepositry.Delete(appointment.Analysis);
                        }

                        _unitOfWork.AppointmentsRepositry.Delete(appointment);
                    }

                    _unitOfWork.PatientsRepositry.Delete(patient);
                    _unitOfWork.Complete();

                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "processed_images", $"Patient_{id}");
                    if (Directory.Exists(folderPath))
                        Directory.Delete(folderPath, recursive: true);
                    TempData["Deleted"] = true;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            return View(patientVm);
        }

    }
}
