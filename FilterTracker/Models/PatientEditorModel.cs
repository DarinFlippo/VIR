using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Diagnostics;

namespace FilterTracker.Models
{
	public class PatientEditorModel : ModelBase
	{
		public PatientEditorModel()
		{
			using (var db = new FilterTrackerEntities())
			{
				var listItems = db.States.AsNoTracking().Select(s => new SelectListItem() { Value = s.Abbreviation, Text = s.Name }).ToList();
				listItems.Insert(0, new SelectListItem() { Text = "", Value = "" });
				States = listItems;

				Genders = new List<SelectListItem>();
				Genders.Add(new SelectListItem { Text = "", Value = "" });
				Genders.Add(new SelectListItem { Text = "Male", Value = "1" });
				Genders.Add(new SelectListItem { Text = "Female", Value = "0" });
			}
		}

		public ViewModelValidationResult IsValid()
		{
			ViewModelValidationResult returned = new ViewModelValidationResult();

			returned.IsValid = true;

			if (OrganizationId <= 0)
			{
				returned.Errors.Add("OrganizationId is required.");
				returned.IsValid = false;
			}

			if (string.IsNullOrEmpty(FirstName))
			{
				returned.Errors.Add("First Name is required.");
				returned.IsValid = false;
			}

			if (string.IsNullOrEmpty(LastName))
			{
				returned.Errors.Add("Last Name is required.");
				returned.IsValid = false;
			}

			//if (string.IsNullOrEmpty(Gender))
			//{
			//    returned.Errors.Add("Gender is required.");
			//    returned.IsValid = false;
			//}

			if (string.IsNullOrEmpty(DateOfBirth))
			{
				returned.Errors.Add("Birth Date is required.");
				returned.IsValid = false;
			}
			else
			{
				DateTime tmp;
				if (!DateTime.TryParse(DateOfBirth, out tmp))
				{
					returned.Errors.Add("Birth Date must be a valid date.");
					returned.IsValid = false;
				}
				else
				{
					if (tmp > DateTime.UtcNow)
					{
						returned.Errors.Add("Birth Date must be a valid date in the past.");
						returned.IsValid = false;
					}
				}
			}

			if (string.IsNullOrEmpty(PrimaryPhoneNumber))
			{
				returned.Errors.Add("Primary Phone Number is required.");
			}

			return returned;
		}

		internal Patient ToPatient()
		{
			Patient returned = new Patient();

			returned.Active = Active;
			returned.AddressLine1 = string.IsNullOrEmpty(AddressLine1) ? null : AddressLine1.Trim();
			returned.AddressLine2 = string.IsNullOrEmpty(AddressLine2) ? null : AddressLine2.Trim();
			returned.City = string.IsNullOrEmpty(City) ? null : City.Trim();

			returned.FirstName = string.IsNullOrEmpty(FirstName) ? null : FirstName.Trim();
			returned.MiddleName = string.IsNullOrEmpty(MiddleName) ? null : MiddleName.Trim();
			returned.LastName = string.IsNullOrEmpty(LastName) ? null : LastName.Trim();
			returned.Gender = SelectedGender;
			returned.Id = Id;
			returned.MRN = string.IsNullOrEmpty(MRN) ? null : MRN.Trim().TrimStart(new char[] { '0' });
			returned.OrganizationId = OrganizationId;
			returned.PrimaryEmailAddress = string.IsNullOrEmpty(PrimaryEmailAddress) ? null : PrimaryEmailAddress?.Trim();
			returned.PrimaryPhoneNumber = string.IsNullOrEmpty(PrimaryPhoneNumber) ? null : PrimaryPhoneNumber.Trim();
			returned.SecondaryEmailAddress = string.IsNullOrEmpty(SecondaryEmailAddress) ? null : SecondaryEmailAddress.Trim();
			returned.SecondaryPhoneNumber = string.IsNullOrEmpty(SecondaryPhoneNumber) ? null : SecondaryPhoneNumber.Trim();
			returned.State = string.IsNullOrEmpty(State) ? null : State.Trim();
			returned.Zipcode = string.IsNullOrEmpty(Zipcode) ? null : Zipcode.Trim();


			if (!string.IsNullOrEmpty(DateOfBirth))
			{
				DateTime tmp;
				if (DateTime.TryParse(DateOfBirth.Trim(), out tmp))
				{
					returned.DateOfBirth = tmp.Date;
				}
			}

			if (!string.IsNullOrEmpty(DeceasedDate))
			{
				DateTime tmp;
				if (DateTime.TryParse(DeceasedDate.Trim(), out tmp))
				{
					returned.DeceasedDate = tmp.Date;
				}
			}



			return returned;
		}

		public IEnumerable<SelectListItem> States { get; private set; }

		public int Id { get; set; }

		[Required()]
		public int OrganizationId { get; set; }

		[MaxLength(2000)]
		[Display(Name = "Medical Record Number")]
		public string MRN { get; set; }

		internal void PopulateFromPatient(Patient patient)
		{
			this.Active = patient.Active;
			this.AddressLine1 = patient.AddressLine1;
			this.AddressLine2 = patient.AddressLine2;
			this.City = patient.City;
			this.DateOfBirth = patient.DateOfBirth.ToShortDateString();
			this.FirstName = patient.FirstName;
			this.SelectedGender = patient.Gender;
			this.Id = patient.Id;
			this.LastName = patient.LastName;
			this.MiddleName = patient.MiddleName;
			this.MRN = patient.MRN;
			this.OrganizationId = patient.OrganizationId;
			this.PrimaryEmailAddress = patient.PrimaryEmailAddress;
			this.PrimaryPhoneNumber = patient.PrimaryPhoneNumber;
			this.SecondaryEmailAddress = patient.SecondaryEmailAddress;
			this.SecondaryPhoneNumber = patient.SecondaryPhoneNumber;
			this.State = patient.State;
			this.Zipcode = patient.Zipcode;
			this.DeceasedDate = patient.DeceasedDate.HasValue ? patient.DeceasedDate.Value.ToShortDateString() : null;
		}

		[Display(Name = "First Name")]
		[Required()]
		[MaxLength(100)]
		public string FirstName { get; set; }

		[Display(Name = "Middle Name")]
		[MaxLength(100)]
		public string MiddleName { get; set; }

		[Display(Name = "Last Name")]
		[Required()]
		[MaxLength(100)]
		public string LastName { get; set; }

		private DateTime? _dateofbirth;

		[Required()]
		[Display(Name = "Birthdate")]
		[DataType(DataType.Date)]
		[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
		public string DateOfBirth
		{
			get
			{
				return _dateofbirth.HasValue ? _dateofbirth.Value.ToShortDateString() : null;
			}

			set
			{
				DateTime tmp;
				if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out tmp))
				{
					_dateofbirth = tmp;
				}
			}
		}

		public string DateOfBirthFormatted
		{
			get
			{
				if (_dateofbirth.HasValue)
					return $"{_dateofbirth.Value.Year}-{_dateofbirth.Value.Month.ToString("D2")}-{_dateofbirth.Value.Day.ToString("D2")}";

				return "";
			}
		}

		public string DeceasedDateFormatted
		{
			get
			{
				if (_deceaseddate.HasValue)
					return $"{_deceaseddate.Value.Year}-{_deceaseddate.Value.Month.ToString("D2")}-{_deceaseddate.Value.Day.ToString("D2")}";

				return "";
			}
		}

		//[Required()]
		//[MaxLength(20)]
		public List<SelectListItem> Genders { get; set; }

		[Display(Name = "Gender")]
		public int? SelectedGender { get; set; }

		[Display(Name = "Address Line 1")]
		[MaxLength(200)]
		public string AddressLine1 { get; set; }

		[Display(Name = "Address Line 2")]
		[MaxLength(200)]
		public string AddressLine2 { get; set; }

		[MaxLength(50)]
		public string City { get; set; }

		[MaxLength(2)]
		public string State { get; set; }

		[DataType(DataType.PostalCode)]
		[MaxLength(10)]
		public string Zipcode { get; set; }

		[Phone]
		[Required()]
		[Display(Name = ("Primary Phone No."))]
		[MaxLength(20)]
		public string PrimaryPhoneNumber { get; set; }

		[Phone]
		[Display(Name = ("Secondary Phone No."))]
		[MaxLength(20)]
		public string SecondaryPhoneNumber { get; set; }

		[EmailAddress()]
		[Display(Name = ("Primary Email"))]
		[MaxLength(200)]
		public string PrimaryEmailAddress { get; set; }


		[EmailAddress()]
		[DataType(DataType.EmailAddress)]
		[Display(Name = ("Secondary Email"))]
		[MaxLength(200)]
		public string SecondaryEmailAddress { get; set; }

		private DateTime? _deceaseddate;
		[Display(Name = "Deceased Date")]
		[DataType(DataType.Date)]
		[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
		public string DeceasedDate
		{
			get
			{
				return _deceaseddate.HasValue ? _deceaseddate.Value.ToShortDateString() : null;
			}

			set
			{
				DateTime tmp;
				if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out tmp))
				{
					_deceaseddate = tmp;
				}
			}
		}


		[Display(Name = ("Active"))]
		public bool Active { get; set; }
	}
}