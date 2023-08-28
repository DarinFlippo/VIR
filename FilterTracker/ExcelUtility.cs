using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Extensions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

using CellType = NPOI.SS.UserModel.CellType;

namespace FilterTracker
{
	public class ExcelUtility
	{
		public static MemoryStream GetStreamFromDataSet(DataSet ds)
		{
			MemoryStream memoryStream = SpreadsheetReader.Create();
			using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open((Stream)memoryStream, true))
			{
				for (int index1 = 0; index1 < ds.Tables.Count; ++index1)
				{
					WorksheetPart worksheetPart = spreadsheet.WorkbookPart.AddNewPart<WorksheetPart>();
					worksheetPart.Worksheet = new Worksheet(new OpenXmlElement[1]
					{
						(OpenXmlElement) new SheetData()
					});

					((OpenXmlPartRootElement)worksheetPart.Worksheet).Save();

					Sheets firstChild = spreadsheet.WorkbookPart.Workbook.GetFirstChild<Sheets>();
					string idOfPart = spreadsheet.WorkbookPart.GetIdOfPart((OpenXmlPart)worksheetPart);
					uint num1 = (uint)index1;

					if (Enumerable.Count<Sheet>(firstChild.Elements<Sheet>()) > 0)
						num1 = Enumerable.Max<uint>(Enumerable.Select<Sheet, uint>(firstChild.Elements<Sheet>(), (Func<Sheet, uint>)(s => s.SheetId.Value))) + 1U;

					Sheet sheet = new Sheet()
					{
						Id = (StringValue)idOfPart,
						SheetId = (UInt32Value)num1,
						Name = (StringValue)ds.Tables[index1].TableName
					};
					firstChild.Append(new OpenXmlElement[1]
					{
					(OpenXmlElement) sheet
					});

					WorksheetWriter worksheetWriter = new WorksheetWriter(spreadsheet, worksheetPart);
					SpreadsheetStyle defaultStyle = SpreadsheetReader.GetDefaultStyle(spreadsheet);
					defaultStyle.SetBorder("000000", BorderStyleValues.Thin);
					defaultStyle.IsBold = true;
					for (int index2 = 0; index2 < ds.Tables[index1].Columns.Count; ++index2)
					{
						string excelColumnValue = ExcelUtility.GetExcelColumnValue(index2 + 1);
						worksheetWriter.PasteText(excelColumnValue + "1", ds.Tables[index1].Columns[index2].ColumnName, defaultStyle);
					}
					defaultStyle.IsBold = false;
					int num2 = (int)worksheetWriter.PasteDataTable(ds.Tables[index1], "A2", defaultStyle);
					((OpenXmlPartRootElement)spreadsheet.WorkbookPart.Workbook).Save();
				}
				spreadsheet.WorkbookPart.Workbook.Sheets.FirstChild.Remove();
				spreadsheet.WorkbookPart.Workbook.Sheets.FirstChild.Remove();
				spreadsheet.WorkbookPart.Workbook.Sheets.FirstChild.Remove();
			}
			return memoryStream;
		}

		private static string GetExcelColumnValue(int columnNumber)
		{
			if (columnNumber <= 26)
				return ((char)(columnNumber + 64)).ToString();
			--columnNumber;
			return ExcelUtility.GetExcelColumnValue(columnNumber / 26) + ExcelUtility.GetExcelColumnValue(columnNumber % 26 + 1);
		}
	}

	public class XLSImporter
	{
		/*
    LAST NAME	
	FIRST NAME	
	MIDDLE NAME	
	BIRTH DATE	
	MRN	
	HOME PHONE	
	ADDRESS 1	
	ADDRESS 2	
	CITY	
	STATE NAME	
	COUNTY NAME	
	COUNTRY	
	ZIP CODE	
	IVC FILTER PLACEMENT DATE
    Benton	Cynthia	Wester	 27-Feb-1952	00302226	501-350-3186	21 Markham Place Cir		LITTLE ROCK	Arkansas	PULASKI	United States of America	72211-0000	 02-Oct-2020  16:19 
*/
		HSSFWorkbook _book;
		public List<InputRow> Data = new List<InputRow>();

		public XLSImporter(System.IO.Stream input)
		{
			try
			{
				_book = new HSSFWorkbook(input);

				ISheet sheet = _book.GetSheetAt(0);
				// Start processing file at row 3.
				for (int i = 2; i <= sheet.LastRowNum; i++)
				{

					var row = sheet.GetRow(i);
					if (row != null) //null is when the row only contains empty cells 
					{
						// our "import file" has page numbers in the first cell at times...
						// we have to skip those rows where cell 0 is a number.
						var cell = row.GetCell(0);
						if (cell != null)
						{
							var val = GetCellAsString(cell);
							if (!string.IsNullOrEmpty(val) && val.IsNumeric())
								continue;
						}

						InputRow added = new InputRow(row);
						Data.Add(added);

					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				throw ex;

			}

		}

		DateTime _refdate = new DateTime(1900, 1, 1);

		private static string GetCellAsString(ICell cell)
		{
			if (cell == null)
				return "";

			switch (cell.CellType)
			{
				case NPOI.SS.UserModel.CellType.Boolean:
					return cell.BooleanCellValue.ToString();
				case NPOI.SS.UserModel.CellType.Error:
					return cell.ErrorCellValue.ToString();
				case NPOI.SS.UserModel.CellType.Formula:
					return cell.CellFormula.ToString();
				case NPOI.SS.UserModel.CellType.Numeric:
				{
					Logger.Log.Debug($"Cell value:{cell.NumericCellValue.ToString()}");

					return DateTime.Parse("1/1/1990")
						.AddDays(cell.NumericCellValue)
						.ToShortDateString();
				}
				case NPOI.SS.UserModel.CellType.String:
					return cell.StringCellValue.ToString();
				case NPOI.SS.UserModel.CellType.Blank:
				case NPOI.SS.UserModel.CellType.Unknown:
				default:
					return "";
			}
		}

		public class ImportFailureRow : InputRow
		{
			public string FailureReason { get; set; }

			public ImportFailureRow(InputRow source) : base(null)
			{
				this.Address1 = source.Address1;
				this.Address2 = source.Address2;
				this.BirthDate = source.BirthDate;
				this.City = source.City;
				this.FirstName = source.FirstName;
				this.HomePhone = source.HomePhone;
				this.LastName = source.LastName;
				this.MiddleName = source.MiddleName;
				this.MRN = source.MRN;
				this.PlacementDate = source.PlacementDate;
				this.State = source.State;
				this.Zipcode = source.Zipcode;
			}
		}

		public class InputRow
		{
			public InputRow(IRow row)
			{
				if (row != null)
				{
					try
					{
						LastName = GetCellAsString(row.GetCell(0)) ?? "";
						FirstName = GetCellAsString(row.GetCell(1)) ?? "";
						MiddleName = GetCellAsString(row.GetCell(2)) ?? "";
						MRN = GetCellAsString(row.GetCell(4)) ?? "";
						HomePhone = GetCellAsString(row.GetCell(5)) ?? "";
						Address1 = GetCellAsString(row.GetCell(6)) ?? "";
						Address2 = GetCellAsString(row.GetCell(7)) ?? "";
						City = GetCellAsString(row.GetCell(8)) ?? "";
						State = GetCellAsString(row.GetCell(9)) ?? "";
						Zipcode = GetCellAsString(row.GetCell(12)) ?? "";

						DateTime dtmp;
						string stmp;
						stmp = GetCellAsString(row.GetCell(3));
						if (!string.IsNullOrEmpty(stmp))
						{
							if (DateTime.TryParse(stmp, out dtmp))
								BirthDate = dtmp.Date;
						}

						stmp = GetCellAsString(row.GetCell(13));
						if (!string.IsNullOrEmpty(stmp))
						{
							if (DateTime.TryParse(stmp, out dtmp))
								PlacementDate = dtmp.Date;
						}
					}
					catch (Exception ex)
					{
						Logger.LogException(ex);
						throw ex;
					}
				}
			}
			public string LastName { get; set; }
			public string FirstName { get; set; }
			public string MiddleName { get; set; }
			public DateTime BirthDate { get; set; }
			public string MRN { get; set; }

			public string HomePhone { get; set; }
			public string Address1 { get; set; }
			public string Address2 { get; set; }
			public string City { get; set; }
			public string State { get; set; }
			public string Zipcode { get; set; }
			public DateTime? PlacementDate { get; set; }
		}

	}

}