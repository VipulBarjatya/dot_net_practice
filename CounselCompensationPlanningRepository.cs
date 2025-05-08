using DocumentFormat.OpenXml.Bibliography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QlikReport.Interface.Compensation;
using QlikReport.Interface.Employee;
using ReportingPortalCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Xml.Linq;

namespace QlikReport.Repository.Compensation
{
    public class CounselCompensationPlanningRepository : ICounselCompensationPlanning
    {
        private readonly ICommonClass _commonClass;
        private readonly IService _service;
        private readonly ICompensationCommon _compensationCommon;
        private readonly IRedisCacheData _redisCacheData;
        private readonly IStaffingRatios _staffingRatios;
        private readonly IHoliday _holiday;
        public CounselCompensationPlanningRepository(ICommonClass commonClass, IService service, ICompensationCommon compensationCommon, IRedisCacheData redisCacheData, IStaffingRatios staffingRatios, IHoliday holiday)
        {
            this._commonClass = commonClass;
            this._service = service;
            _compensationCommon = compensationCommon;
            this._redisCacheData = redisCacheData;
            this._staffingRatios = staffingRatios;
            this._holiday = holiday;
        }

        public CounselCompensationPlanningModel GetCounselCompensationPlanning(string suserID, string sUserDirectory, DataTable dtRoleDetails, int iRoleID, int iMenuID, DataTable dtAction, bool isDateSetManual, int iReportID, int oriRoleID, bool bIsCallFilter, int Year)
        {
            CounselCompensationPlanningModel objCounselCompensationPlanningModel = new CounselCompensationPlanningModel();
            List<EmployeeStaffingFTECal> lstEmpFTE = new List<EmployeeStaffingFTECal>();
            try
            {
                #region Filter and Action
                if (bIsCallFilter == true)
                {
                    StaffingRatiosModel staffingRatiosModel = _staffingRatios.GetStaffingRatios(dtRoleDetails, iRoleID, iMenuID);

                    objCounselCompensationPlanningModel.strOfficeCode = staffingRatiosModel.strOfficeCode;
                    objCounselCompensationPlanningModel.strEmployeeName = staffingRatiosModel.strEmployeeName;
                    objCounselCompensationPlanningModel.strAderantDepartment = staffingRatiosModel.strAderantDepartment;
                    objCounselCompensationPlanningModel.lstFilterDetails = staffingRatiosModel.lstFilterDetails;
                    objCounselCompensationPlanningModel.strClassYear = staffingRatiosModel.strClassYear;
                    objCounselCompensationPlanningModel.strGLManager = staffingRatiosModel.strGLManager;

                    objCounselCompensationPlanningModel.strFilterSelection = staffingRatiosModel.strFilterSelection;
                    objCounselCompensationPlanningModel.sStarDate = staffingRatiosModel.sStarDate;
                    objCounselCompensationPlanningModel.sEndDate = staffingRatiosModel.sEndDate;
                }
                #endregion

                #region Year Tab
                objCounselCompensationPlanningModel.Year = Year;
                objCounselCompensationPlanningModel.lstYear = _commonClass.GetYearTo(objCounselCompensationPlanningModel.YearCurrent);
                objCounselCompensationPlanningModel.YearCurrent = Year;

                List<CompensationFreezeDetailsModel> lstCompensationFreezeDetails = _compensationCommon.GetCompensationFreezeDetailsList((int)GlobalVariable.CompForm.Counsel);
                if (lstCompensationFreezeDetails != null && lstCompensationFreezeDetails.Count > 0)
                {
                    foreach (YearModel objModel in objCounselCompensationPlanningModel.lstYear)
                    {
                        if (lstCompensationFreezeDetails.Where(a => a.Year.ToString() == objModel.Year).ToList().Count > 0)
                            objModel.HasFreezeData = true;
                    }
                }

                objCounselCompensationPlanningModel.lstYearBonus = _commonClass.GetYearTo(Convert.ToInt32(_service.ExecuteScaler("EXEC[usp_Bonus_Class_Year] @Action ='Get', @Criteria ='Get_Bonus_Min_Year', @CompensationFormID = 2")));
                #endregion

                #region Get start & end date
                DateTime dStartDate = new DateTime(Year - 1, 12, 1);
                DateTime FTEndDate = _commonClass.GenEndDate_Compensation(Year);
                DateTime dEndDate = _commonClass.EndDateOfMonth(Convert.ToDateTime(objCounselCompensationPlanningModel.sEndDate).Year, Convert.ToDateTime(objCounselCompensationPlanningModel.sEndDate).Month);
                //DateTime apiEndDate = _commonClass.GenEndDate_Compensation(Year);
                //DateTime endDate = DateTime.Now;
                //if (DateTime.Now.Year != Year)
                //{
                //    endDate = new DateTime(Year, 11, 30);
                //}
                #endregion

                #region Get Holidays list
                List<Holiday> lstHolidays = _holiday.GetHolidayModel().lstHoliday;
                #endregion

                #region Get Compensation Employee Salary From DB
                List<EmployeeSalary> lstESDB = _commonClass.GetCompensationEmploye(Year);
                #endregion               

                #region Employee Leave Details
                DataTable dtEmpLeaveDetails = _service.DAGetDataSet("EXEC[usp_Employee] @Action ='Get', @Criteria ='Get_Employee_Leave_Details',@SearchString=''", true).Tables[0];
                List<EmployeeLeaveDetails> lstEmpLeaveDetails = CommonClass.DataTableToList<EmployeeLeaveDetails>(dtEmpLeaveDetails);
                #endregion

                #region Bind Councel Compensation Details
                //if FeeCollection > 500000  then DiscretionaryBonusCollections is set 2
                //annual Hour > 2000 then DiscretionaryBonusBillables is set 2
                //annual Hour=>((BillableCredit -BillableHours) + AnnualizedHours);

                #region Get Redis FTE and Bind NEP Employee Details  
                DateTime startdate = new DateTime(Year - 3, 12, 1);
                DateTime eEndDate = new DateTime(Year, DateTime.Now.Month, 1).AddMonths(0).AddDays(-1);
                if (Year != DateTime.Now.Year)
                {
                    eEndDate = new DateTime(Year, 11, 30);
                }
                List<AllMonthData> lstLastThreeAllMonthData = _redisCacheData.GetRedisData(startdate.ToString("yyy-MMM"), eEndDate.ToString("yyy-MMM"));
                lstLastThreeAllMonthData.ForEach(i =>
                {
                    if (i.CalendarYear > 0 && i.MonthOfYear > 0)
                    {
                        i.YearMon = i.CalendarYear + "-" + _commonClass.GetMonthShortName(i.MonthOfYear);
                        i.ShortMonthName = _commonClass.GetMonthShortName(i.MonthOfYear);
                        i.FirstDateOfMonth = new DateTime(i.CalendarYear, i.MonthOfYear, 1);
                    }
                    if (!string.IsNullOrEmpty(i.MatterCode))
                        i.MatterCode = Convert.ToInt32(i.MatterCode).ToString("D4");
                });

                lstEmpFTE = _redisCacheData.GetRedisStaffingFTEData(dStartDate.ToString("yyyy-MMM"), FTEndDate.ToString("yyyy-MMM"));

                #region User Penality
                // Below code added by Ramawatar Sharma
                //on 18 Dec, 2024 to fetch penality in single call instead of fetching for single user
                DateTime ClosedDate = DateTime.Now;
                if (DateTime.Now.Year != Year)
                {
                    ClosedDate = new DateTime(Year, 12, 31);
                }

                DataTable dtPenality = _service.DAGetDataSet("Exec [usp_Qlik_User_Penality] @Action='Get', @Criteria='Get_User_Penality_For_Compensation_All', @WorkDate='" + ClosedDate.ToString() + "'").Tables[0];
                #endregion

                // By Alka, ON 3 march,2025
                //here get paid bonus data with GLAccount = 'Adp-bonus' and adppaytype will be HoursBonus 
                List<ExpensesAnalysis> lstExpensesData = _redisCacheData.GetRedisDataExpenses(ClosedDate.ToString("yyy-MMM"), ClosedDate.ToString("yyy-MMM"));
                lstExpensesData = lstExpensesData.Where(a => a.glAccount == "ADP-payroll" || a.glAccount == "ADP-bonus").ToList();


                CounselCompensationPlanningModel objCounselCompensationPlanningModelData = BindCounselStaffDetails(lstEmpFTE, lstEmpLeaveDetails, lstESDB, lstHolidays, lstLastThreeAllMonthData, oriRoleID, Year, dStartDate, dEndDate, FTEndDate, lstExpensesData, false, dtPenality);
                objCounselCompensationPlanningModel.lstCompensation_Extra_Column = objCounselCompensationPlanningModelData.lstCompensation_Extra_Column;
                objCounselCompensationPlanningModel.lstBonusVersion = objCounselCompensationPlanningModelData.lstBonusVersion;
                objCounselCompensationPlanningModel.lstAssociateBonus = objCounselCompensationPlanningModelData.lstAssociateBonus;
                objCounselCompensationPlanningModel.lstCounselCompensationPlanning = objCounselCompensationPlanningModelData.lstCounselCompensationPlanning;

                #endregion
                #endregion
            }
            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
            }

            #region Get Counsel History of Active Attorny 
            List<CounselCompensationHistory> lstCounselHistoryDetails = new List<CounselCompensationHistory>();
            List<CounselCompensationHistory> lstCounselHistory = GetCounselHistoryDetails();
            //List<OriAttornyActive> lstAttornyActive = _commonClass.GetOriAttornyActive_RankCode();

            foreach (CounselCompensationHistory objCounselHistory in lstCounselHistory)
            {
                //var objOriAttr = lstEmpFTE.Find(m => m.EmpName == objCounselHistory.Name.Trim());
                if (objCounselCompensationPlanningModel.lstCounselCompensationPlanning != null)
                {
                    CounselCompensationPlanningModel objNEP = objCounselCompensationPlanningModel.lstCounselCompensationPlanning.Find(m => m.Name == objCounselHistory.Name.Trim());
                    lstCounselHistoryDetails.Add(objCounselHistory);
                }
            }
            #endregion

            objCounselCompensationPlanningModel.lstCounselCompensationHistory = lstCounselHistoryDetails;
            return objCounselCompensationPlanningModel;
        }

        public CounselCompensationPlanningModel BindCounselStaffDetails(List<EmployeeStaffingFTECal> lstEmpFTE, List<EmployeeLeaveDetails> lstEmpLeaveDetails, List<EmployeeSalary> lstESDB
           , List<Holiday> lstHolidays, List<AllMonthData> lstLastThreeAllMonthData, int oriRoleID, int Year, DateTime dStartDate, DateTime dEndDate, DateTime FTEndDate, List<ExpensesAnalysis> lstExpensesData, bool isForCompensationSummary = false, DataTable dtPenality = null)
        {
            CounselCompensationPlanningModel objCounselCompensationPlanningModel = new CounselCompensationPlanningModel();
            List<CounselCompensationPlanningModel> lstModel = new List<CounselCompensationPlanningModel>();

            #region Get Counsel Compensation from Database
            List<CounselCompensationPlanningModel> lstCCDB = new List<CounselCompensationPlanningModel>();
            DataTable dtTC = _service.DAGetDataSet("EXEC[usp_Compensation_Counsel] @Action ='Get', @Criteria ='Get_Counsel_Compensation_Details', @Year=" + Year).Tables[0];
            if (dtTC != null && dtTC.Rows.Count > 0)
            {
                lstCCDB = CommonClass.ConvertDataTable<CounselCompensationPlanningModel>(dtTC);
            }
            #endregion

            #region Get YE Score and Mid Year Counsel Compensation from Database
            List<CounselCompensationPlanningModel> lstCounselYEScore = new List<CounselCompensationPlanningModel>();
            DataTable dtYEScore = _service.DAGetDataSet("EXEC[usp_Compensation_Counsel] @Action ='Get', @Criteria ='Get_All_Counsel_Compensation_YEScore'").Tables[0];
            if (dtYEScore != null && dtYEScore.Rows.Count > 0)
            {
                lstCounselYEScore = CommonClass.ConvertDataTable<CounselCompensationPlanningModel>(dtYEScore);
            }
            #endregion

            #region Get Counsel Compensation Extra Column from DB
            List<Compensation_Extra_Column> lstCECDB = new List<Compensation_Extra_Column>();
            DataTable dtCEC = _service.DAGetDataSet("EXEC[usp_Compensation_Class_Year] @Action ='Get', @Criteria ='Get_Counsel_Compensation_Extra_Column',@Year = " + Year).Tables[0];
            if (dtCEC != null && dtCEC.Rows.Count > 0)
            {
                lstCECDB = dtCEC.DataTableToList<Compensation_Extra_Column>();
                lstCECDB = lstCECDB.OrderBy(m => m.SeqNo).ToList();
            }
            #endregion

            List<TimekeeperCompensationPerformanceScoreModel> dtPerformanceScore = new List<TimekeeperCompensationPerformanceScoreModel>();
            if (isForCompensationSummary == false)
            {
                dtPerformanceScore = _commonClass.GetPerformanceScore_Excel_Compensation();
            }

            lstEmpFTE = lstEmpFTE.FindAll(a => a.ToDateActual >= FTEndDate && a.FromDate <= FTEndDate);

            var lstFTEDataCounsel = lstEmpFTE.Where(a => a.AxinnPosition != null && a.AxinnPosition.Trim() == "Counsel").GroupBy(x => new
            {
                x.EmpName,
                x.EmployeeID,
                x.EmployeeCodeAderant
            }).Select(m => new CounselCompensationPlanningModel
            {
                Name = m.Key.EmpName,
                EMPUID = m.Key.EmployeeID,
                EmployeeCode = m.Key.EmployeeCodeAderant
            }).Distinct().ToList();

            if (lstCCDB != null && lstCCDB.Count > 0)
            {
                lstFTEDataCounsel = lstFTEDataCounsel.Concat(lstCCDB.Where(s => s.IsNewEmployee == true)).ToList();
            }

            foreach (var objEmp in lstFTEDataCounsel)
            {
                List<EmployeeLeaveDetails> lstEmpLeave = lstEmpLeaveDetails.FindAll(m => m.EmployeeID == objEmp.EMPUID);
                CounselCompensationPlanningModel objCCDB = lstCCDB.Find(m => m.EmployeeCode.Trim() == objEmp.EmployeeCode.Trim());
                CounselCompensationPlanningModel objCC = Get_Counsel_EmpDetails(objEmp.Name, objEmp.EMPUID, objEmp.EmployeeCode.Trim(), Year, dStartDate, dEndDate, lstEmpLeave, lstHolidays, lstLastThreeAllMonthData, objCCDB, lstEmpFTE, dtPerformanceScore, lstCounselYEScore, lstCECDB, lstESDB, lstExpensesData, isForCompensationSummary, dtPenality);
                if (objCC != null)
                {
                    if (objCC.IsNewEmployee == true)
                    {
                        int nextYear = Year + 1;
                        DateTime YearStartDate = Convert.ToDateTime("01/01/" + nextYear);
                        DateTime YearEndDate = Convert.ToDateTime("12/31/" + nextYear);
                        //Get total year working days
                        Decimal YearDays = _commonClass.Calculate_WorkingDays_CurrentYear(YearStartDate, YearEndDate, lstHolidays);

                        // Get total working days of employee 
                        var HireDate = new[] { objCC.HireDate, new DateTime(nextYear, 1, 1) }.Max();
                        Decimal TotalWorkingDays = _commonClass.Calculate_WorkingDays_CurrentYear(HireDate, YearEndDate, lstHolidays);
                        objCC.LawSchoolGrad = objCCDB.LawSchoolGrad;
                        objCC.NextYearFTE = objCCDB.FTE;
                        objCC.YearDays = YearDays;
                        objCC.TotalWorkingDays = TotalWorkingDays;
                        objCC.BudgetSalary = (objCCDB.Compensation / YearDays) * TotalWorkingDays;
                    }
                    lstModel.Add(objCC);
                }
                //else if (objCCDB != null && objCCDB.IsNewEmployee == true)
                //{

                //    int nextYear = Year + 1;
                //    DateTime YearStartDate = Convert.ToDateTime("01/01/" + nextYear);
                //    DateTime YearEndDate = Convert.ToDateTime("12/31/" + nextYear);
                //    //Get total year working days
                //    Decimal YearDays = _commonClass.Calculate_WorkingDays_CurrentYear(YearStartDate, YearEndDate, lstHolidays);

                //    // Get total working days of employee 
                //    var HireDate = new[] { objCCDB.HireDate, new DateTime(nextYear, 1, 1) }.Max();
                //    Decimal TotalWorkingDays = _commonClass.Calculate_WorkingDays_CurrentYear(HireDate, YearEndDate, lstHolidays);
                //    objCCDB.NextYearFTE = objCCDB.FTE;
                //    objCCDB.YearDays = YearDays;
                //    objCCDB.TotalWorkingDays = TotalWorkingDays;
                //    objCCDB.BudgetSalary = (objCCDB.Compensation / YearDays) * TotalWorkingDays;
                //    lstModel.Add(objCCDB);
                //}

            }

            #region Bind Emp Details OLD
            //foreach (var objEmp in lstEmpName)
            //{
            //    string empName = CommonClass.GetSplitString(objEmp.EmployeName);
            //    BaseCompensationPlanning objCPM = lstCP.Find(m => m.EMPUID == objEmp.EMPUID);
            //    WtkprPracticegrpOfficeBillableHrs objPOB = lstPracOffBill.Find(m => m.wTkpr == objEmp.EmployeName);
            //    WorkingTimekeepers_Hours objBillProbono = lstBillProbono.Find(m => m.EMPUID == objEmp.EMPUID);
            //    CounselCompensationPlanningModel objCCB = lstCCDB.Find(m => m.Name == objEmp.EmployeName);
            //    List<WorkingTimekeepers_Hours_Collection> lstObjBillCollection = lstBillColl.Where(m => m.EMPUID == objEmp.EMPUID).ToList();

            //    DateTime sStartDate = new DateTime(Year - 1, 12, 1);
            //    DateTime StartDateFromDecLastYear = new DateTime(Year - 1, 12, 1);

            //    //By alka-Date-06-08-2023---Here Getting Employee Salary detail using EmployeeCode
            //    List<CompensationSalaryIncrease> lstCSFilter = _commonClass.GetEmployeSalaryDetail(lstESDB, objEmp.EmployeeCode);

            //    if (lstCSFilter.Count > 0)
            //    {
            //        CounselCompensationPlanningModel objCCP = new CounselCompensationPlanningModel();
            //        objCCP.Name = objEmp.EmployeName;
            //        objCCP.lstCompensationSalaryIncrease = lstCSFilter;
            //        List<EmployeeLeaveDetails> lstELD = lstEmployeeLeaveDetails.FindAll(m => m.EMPUID == objEmp.EMPUID);

            //        if (lstObjBillCollection.Count > 0)
            //        {
            //            lstObjBillCollection = lstObjBillCollection.OrderByDescending(a => a.Year).ToList();
            //            lstObjBillCollection.Where(a => a.Month == 12).ToList().ForEach(i =>
            //            {
            //                i.Year = i.Year + 1;
            //            });
            //            objCCP.lstCompensationPerformanceBillColl = lstObjBillCollection.OrderBy(a => a.Year).ToList();
            //        }

            //        if (objCPM != null)
            //        {
            //            objCCP.NextAxinnRole = objCPM.TitleCurrentYear;
            //            objCCP.RoleCurrentYear = objCPM.TitleCurrentYear;
            //            objCCP.AxinnYrCurrentYear = objCPM.AxinnYear;
            //            objCCP.Manager = objCPM.Manager;
            //            objCCP.HireDate = objCPM.HireDate;
            //            objCCP.LawSchoolGrad = objCPM.LawSchollGrad;
            //            objCCP.IsCustomAdded = objCPM.IsCustomAdded;
            //            objCCP.PositionChangeDate = objCPM.PositionChangeDate;
            //            if (objCPM.FeeCollection > 500000)
            //                objCCP.DiscretionaryBonusCollections = 2;
            //        }

            //        if (objCCP.HireDate > sStartDate)
            //        {
            //            sStartDate = objCCP.HireDate;
            //        }

            //        if (objPOB != null)
            //        {
            //            objCCP.PracticeGroup = objPOB.PracticeGroup;
            //            objCCP.Office = objPOB.TkprOffice;
            //            objCCP.YTDBillableHours = objPOB.billableHours;
            //        }

            //        if (objCPM != null)
            //        {
            //            //modified by Ramawatar Sharma
            //            //on 19/09/2023 
            //            //previous we were passing dEndDate , which value were previous month last date
            //            //now passing november month last date
            //            objCCP.FTE = _commonClass.Calculate_FTE_Hiredate(objCPM.HireDate, (int)GlobalVariable.CompForm.Counsel, lstELD, lstHolidays, new DateTime(Year, 11, 30), Year);
            //            objCCP.NextYearFTE = 1;
            //            objCCP.lstEmployeeLeaveDetails = _commonClass.GetEmployeeleaveDetails(lstELD, lstHolidays, dEndDate, Year);
            //        }

            //        if (objBillProbono != null)
            //        {
            //            objCCP.BillableHours = objBillProbono.BillableHours;
            //            objCCP.NonBillable = objBillProbono.NonBillable;
            //            objCCP.ProBono = objBillProbono.ProBono;
            //            objCCP.DEI = objBillProbono.DEI;
            //            objCCP.GeneralCounsel = objBillProbono.GeneralCounsel;
            //            objCCP.TravelHours = objBillProbono.TravelHours;
            //            objCCP.BillableCredit = objBillProbono.BillableCredit;

            //            objCCP.annualHour = ((objBillProbono.BillableCredit - objBillProbono.BillableHours) + _commonClass.GetAnnualizedHours(objBillProbono.BillableHours, lstELD, sStartDate, dEndDate, lstHolidays));
            //            objCCP.FinalAdjustmentHrs = objCCP.annualHour;

            //            if (objCCP.annualHour > 2000)
            //                objCCP.DiscretionaryBonusBillables = 2;

            //            objCCP.ProfessionalDeveloment = objBillProbono.ProfessionalDeveloment;
            //        }

            //        objCCP.BonusesCurrentYear = objCCP.BonusesCurrentYear;
            //        objCCP.CorrectedBillableHours = 0;
            //        objCCP.ProjectedNextYearBeforeDiscretionaryBonus = 0;

            //        if (objCCB != null)
            //        {
            //            objCCP.ID = objCCB.ID;
            //            objCCP.DiscretionaryBonusCollections = objCCB.DiscretionaryBonusCollections;
            //            objCCP.DiscretionaryBonusBillables = objCCB.DiscretionaryBonusBillables;
            //            objCCP.AssocReferralProgram = objCCB.AssocReferralProgram;
            //            objCCP.NextAxinnRole = objCCB.NextAxinnRole;
            //            objCCP.NextAxinnBase = objCCB.NextAxinnBase;
            //            objCCP.PGChairComments = objCCB.PGChairComments;
            //            objCCP.NextYearFTE = objCCB.FTE;
            //            objCCP.Adjustment = objCCB.Adjustment;
            //            objCCP.AdjustmentComments = objCCB.AdjustmentComments;
            //            objCCP.NextYearAdj = objCCB.NextYearAdj;
            //            objCCP.NextYearAdjComment = objCCB.NextYearAdjComment;
            //            objCCP.SpecialBonus = objCCB.SpecialBonus;
            //            objCCP.Compensation = objCCB.Compensation;
            //            objCCP.CurrentYearYEScore = objCCB.CurrentYearYEScore;
            //            objCCP.CurrentYearMidYearScore = objCCB.CurrentYearMidYearScore;

            //            objCCP.AdjustmentHrs = objCCB.AdjustmentHrs;
            //            objCCP.AdjustmentHrsComments = objCCB.AdjustmentHrsComments;
            //            objCCP.IsAdjustedHrsPercentage = objCCB.IsAdjustedHrsPercentage;
            //            objCCP.IsAdjusted_YearEnd_Percentage = objCCB.IsAdjusted_YearEnd_Percentage;

            //            if (objCCB.IsAdjustedHrsPercentage == true)
            //            {
            //                objCCP.FinalAdjustmentHrs = (objCCP.FinalAdjustmentHrs * objCCP.AdjustmentHrs) / 100;
            //            }
            //            else
            //            {
            //                objCCP.FinalAdjustmentHrs = objCCP.FinalAdjustmentHrs + objCCP.AdjustmentHrs;
            //            }


            //            foreach (Compensation_Extra_Column objCE in lstCECDB)
            //            {
            //                if (objCE.ColumnName == "Column1")
            //                {
            //                    objCCP.Column1 = objCCB.Column1;
            //                }
            //                else if (objCE.ColumnName == "Column2")
            //                {
            //                    objCCP.Column2 = objCCB.Column2;
            //                }
            //                else if (objCE.ColumnName == "Column3")
            //                {
            //                    objCCP.Column3 = objCCB.Column3;
            //                }
            //                else if (objCE.ColumnName == "Column4")
            //                {
            //                    objCCP.Column4 = objCCB.Column4;
            //                }
            //                else if (objCE.ColumnName == "Column5")
            //                {
            //                    objCCP.Column5 = objCCB.Column5;
            //                }
            //                else if (objCE.ColumnName == "Column6")
            //                {
            //                    objCCP.Column6 = objCCB.Column6;
            //                }
            //                else if (objCE.ColumnName == "Column7")
            //                {
            //                    objCCP.Column7 = objCCB.Column7;
            //                }
            //                else if (objCE.ColumnName == "Column8")
            //                {
            //                    objCCP.Column8 = objCCB.Column8;
            //                }
            //                else if (objCE.ColumnName == "Column9")
            //                {
            //                    objCCP.Column9 = objCCB.Column9;
            //                }
            //                else if (objCE.ColumnName == "Column10")
            //                {
            //                    objCCP.Column10 = objCCB.Column10;
            //                }
            //            }
            //        }

            //        if (DateTime.Now.Year != Year)
            //        {
            //            objCCP.YearPenalty = _commonClass.GetPenalityAmount(objEmp.EmployeName, new DateTime(Year, 11, 30).ToString());
            //        }
            //        else
            //        {
            //            objCCP.YearPenalty = _commonClass.GetPenalityAmount(objEmp.EmployeName, DateTime.Now.ToString());
            //        }

            //        objCCP.lstCompensationPerformanceScore = _commonClass.GetPerformanceScore(objEmp.EmployeName, dtPerformanceScore);
            //        objCCP.lstCompensationPerformanceScore = BindYEScoreFromDB(objEmp.EmployeName, objCCP.lstCompensationPerformanceScore, lstCounselYEScore, Year);

            //        lstModel.Add(objCCP);
            //    }
            //} 
            #endregion

            lstModel = lstModel.Where(m => m.RoleCurrentYear != "" && m.RoleCurrentYear != "-").ToList();

            lstModel = lstModel.OrderBy(m => m.Name).ToList();

            #region Get Associate and NEP Compensation Details With Counsel Position Change
            DataTable dtAssociate = _service.DAGetDataSet("EXEC[usp_Compensation_Counsel] @Action ='Get', @Criteria ='Get_Associate_Compensation_Details_With_Councel', @Year=" + Year).Tables[0];

            if (dtAssociate != null && dtAssociate.Rows.Count > 0)
            {
                foreach (DataRow dr in dtAssociate.Rows)
                {
                    var lstFTEDataEmp = lstEmpFTE.Where(a => a.EmpName == dr["Name"].ToString());
                    List<EmployeeLeaveDetails> lstELD = lstEmpLeaveDetails.FindAll(m => m.EmployeName == dr["Name"].ToString());

                    if (lstFTEDataEmp.Count() > 0 && lstModel.Find(a => a.Name == dr["Name"].ToString()) == null)
                    {
                        int EmployeeID = lstFTEDataEmp.FirstOrDefault().EmployeeID;
                        string EmployeeCode = lstFTEDataEmp.FirstOrDefault().EmployeeCodeAderant.Trim();
                        CounselCompensationPlanningModel objCCDB = lstCCDB.Find(m => m.EmployeeCode == dr["EmployeeCode"].ToString());
                        CounselCompensationPlanningModel objCC = Get_Counsel_EmpDetails(dr["Name"].ToString(), EmployeeID, EmployeeCode, Year, dStartDate, dEndDate, lstELD, lstHolidays, lstLastThreeAllMonthData, objCCDB, lstEmpFTE, dtPerformanceScore, lstCounselYEScore, lstCECDB, lstESDB, lstExpensesData, isForCompensationSummary, dtPenality);

                        if (objCC != null)
                        {
                            objCC.RoleCurrentYear = "Counsel";
                            objCC.NextAxinnRole = "Counsel";
                            objCC.AxinnRoleType = dr["AxinnRoleType"].ToString();
                            objCC.IsCustomAdded = true;
                            objCC.PositionChangeDate = Convert.ToDateTime(dr["UpdatedAt"].ToString());
                            // objCC.LU_Compensation_Form_ID = Convert.ToInt32(dr["LU_Compensation_Form_ID"]);
                            lstModel.Add(objCC);
                        }
                    }
                }
            }

            #endregion


            #region Get Counsel Bonus
            List<Associate_Bonus> lstCounselBonus = new List<Associate_Bonus>();
            DataTable dtBonus = _service.DAGetDataSet("EXEC[usp_Bonus_Class_Year] @Action ='Get', @Criteria ='Get_Counsel_Bonus',@FormID=" + 2 + ", @Year=" + Year).Tables[0];
            if (dtBonus != null && dtBonus.Rows.Count > 0)
            {
                lstCounselBonus = CommonClass.ConvertDataTable<Associate_Bonus>(dtBonus);
            }

            int TotalBonusVersion = lstCounselBonus.Count > 0 ? lstCounselBonus.Max(m => m.Version) : 1;
            #endregion

            #region Add Counsel Compensation_Extra_Column
            List<Compensation_Extra_Column> lstCEC = new List<Compensation_Extra_Column>();
            int seqNo = 24;
            Compensation_Extra_Column objCEC = new Compensation_Extra_Column();

            if (isForCompensationSummary == false)
            {
                objCEC = new Compensation_Extra_Column();
                objCEC.Name = "Discretionary Bonus if Collections over $500K (Dec-Nov)";
                objCEC.ColumnName = "Discretionary Bonus if Collections over $500K (Dec-Nov)";
                objCEC.SeqNo = seqNo;
                objCEC.RoleID = oriRoleID;
                lstCEC.Add(objCEC);
                seqNo++;

                objCEC = new Compensation_Extra_Column();
                objCEC.Name = "Discretionary Bonus if Billables over 2,000  (Dec-Nov)";
                objCEC.ColumnName = "Discretionary Bonus if Billables over 2,000  (Dec-Nov)";
                objCEC.SeqNo = seqNo;
                objCEC.RoleID = oriRoleID;
                lstCEC.Add(objCEC);
                seqNo++;

                //objCEC = new Compensation_Extra_Column();
                //objCEC.Name = "Assoc Referral program";
                //objCEC.ColumnName = "Assoc Referral program";
                //objCEC.SeqNo = seqNo;
                //objCEC.RoleID = oriRoleID;
                //lstCEC.Add(objCEC);
                //seqNo++;
            }

            #region Year End version
            List<int> lstBonusVersion = new List<int>();
            for (int i = 1; i <= TotalBonusVersion; i++)
            {
                List<Associate_Bonus> lstAssociateBonusFilter = lstCounselBonus.Where(m => m.Version == i).ToList();
                Compensation_Extra_Column objCECYE = new Compensation_Extra_Column();

                if (TotalBonusVersion == 1)
                    objCECYE.Name = "Year End";
                else
                    if (i == 1)
                    objCECYE.Name = "Year End";
                else
                    objCECYE.Name = "Year End (" + lstAssociateBonusFilter[0].CreatedAt.ToString("MM/dd/yyyy") + ")";

                objCECYE.Type = "Year End";
                objCECYE.Version = i;
                objCECYE.ColumnName = "YearEnd" + i;
                objCECYE.SeqNo = seqNo;
                objCECYE.RoleID = oriRoleID;
                lstCEC.Add(objCECYE);
                seqNo++;
                lstBonusVersion.Add(i);
            }
            #endregion

            if (isForCompensationSummary == false)
            {
                //Grid Versions Working soon
                Compensation_Extra_Column objCECAD = new Compensation_Extra_Column();
                objCECAD.Name = "Adjustment";
                objCECAD.ColumnName = "Adjustment";
                objCECAD.SeqNo = seqNo;
                objCECAD.RoleID = oriRoleID;
                lstCEC.Add(objCECAD);
                seqNo++;

                Compensation_Extra_Column objCECADC = new Compensation_Extra_Column();
                objCECADC.Name = "Adjustment Comments";
                objCECADC.ColumnName = "Adjustment Comments";
                objCECADC.SeqNo = seqNo;
                objCECADC.RoleID = oriRoleID;
                lstCEC.Add(objCECADC);
                seqNo++;

                Compensation_Extra_Column objCECADYB = new Compensation_Extra_Column();
                objCECADYB.Name = "Adjusted YE Bonus";
                objCECADYB.ColumnName = "Adjusted YE Bonus";
                objCECADYB.SeqNo = seqNo;
                objCECADYB.RoleID = oriRoleID;
                lstCEC.Add(objCECADYB);
                seqNo++;

                Compensation_Extra_Column objCECOB = new Compensation_Extra_Column();
                objCECOB.Name = "Special Bonus";
                objCECOB.ColumnName = "Special Bonus";
                objCECOB.SeqNo = seqNo;
                objCECOB.RoleID = oriRoleID;
                lstCEC.Add(objCECOB);
                seqNo++;
            }

            foreach (Compensation_Extra_Column objCECDB in lstCECDB)
            {
                objCECDB.IsAddColumn = true;
                objCECDB.SeqNo = seqNo;
                objCECDB.RoleID = oriRoleID;
                lstCEC.Add(objCECDB);
                seqNo++;
            }

            if (isForCompensationSummary == false)
            {
                objCEC = new Compensation_Extra_Column();
                objCEC.Name = "Sub-Total";
                objCEC.ColumnName = "Sub-Total";
                objCEC.SeqNo = seqNo;
                objCEC.RoleID = oriRoleID;
                lstCEC.Add(objCEC);
            }
            #endregion

            objCounselCompensationPlanningModel.lstCompensation_Extra_Column = lstCEC;
            objCounselCompensationPlanningModel.lstBonusVersion = lstBonusVersion;
            objCounselCompensationPlanningModel.lstAssociateBonus = lstCounselBonus;
            objCounselCompensationPlanningModel.lstCounselCompensationPlanning = lstModel;

            return objCounselCompensationPlanningModel;
        }
        private CounselCompensationPlanningModel Get_Counsel_EmpDetails(string EmployeeName, int EmployeeID, string EmployeeCode, int Year, DateTime StartDate, DateTime dEndDate, List<EmployeeLeaveDetails> lstELD, List<Holiday> lstHolidays, List<AllMonthData> lstLastThreeAllMonthData, CounselCompensationPlanningModel objCCDB, List<EmployeeStaffingFTECal> lstEmpFTE, List<TimekeeperCompensationPerformanceScoreModel> dtPerformanceScore, List<CounselCompensationPlanningModel> lstCounselYEScore, List<Compensation_Extra_Column> lstCECDB, List<EmployeeSalary> lstESDB, List<ExpensesAnalysis> lstExpensesData, bool isForCompensationSummary, DataTable dtPenality = null)
        {
            CounselCompensationPlanningModel objCC = new CounselCompensationPlanningModel();
            try
            {
                //List<WorkingTimekeepers_Hours_Collection> lstObjBillCollection = lstLastThreeAllMonthData.Where(m => m.EmployeeID == EmployeeID).GroupBy(item => new
                //{
                //    item.CalendarYear
                //})
                //   .Select(group => new WorkingTimekeepers_Hours_Collection
                //   {
                //       Name = group.Select(x => x.EmployeeName).FirstOrDefault(),
                //       Year = Convert.ToInt32(group.Select(x => x.CalendarYear).FirstOrDefault().ToString()),
                //       FeeCollection = group.Sum(k => k.FeeCollection),
                //       BillableHours = group.Sum(a => a.ActualBaseHours),
                //   }).ToList();

                List<WorkingTimekeepers_Hours_Collection> lstObjBillCollection = new List<WorkingTimekeepers_Hours_Collection>();
                List<AllMonthData> employeeData = lstLastThreeAllMonthData.Where(m => m.EmployeeID == EmployeeID).ToList();
                if (employeeData.Count > 0)
                {
                    foreach (int year in employeeData.Select(a => a.CalendarYear).Distinct())
                    {
                        WorkingTimekeepers_Hours_Collection objWorkingTimekeepersHoursCollection = new WorkingTimekeepers_Hours_Collection();
                        objWorkingTimekeepersHoursCollection.Name = employeeData[0].EmployeeName;
                        objWorkingTimekeepersHoursCollection.Year = year;
                        objWorkingTimekeepersHoursCollection.FeeCollection = employeeData.Where(a => a.CalendarYear == year - 1 && a.MonthOfYear == 12).Sum(a => a.FeeCollection)
                                                                                +
                                                                             employeeData.Where(a => a.CalendarYear == year && a.MonthOfYear < 12).Sum(a => a.FeeCollection);

                        objWorkingTimekeepersHoursCollection.BillableHours = employeeData.Where(a => a.CalendarYear == year - 1 && a.MonthOfYear == 12).Sum(a => a.ActualBaseHours)
                                                                                +
                                                                             employeeData.Where(a => a.CalendarYear == year && a.MonthOfYear < 12).Sum(a => a.ActualBaseHours);
                        lstObjBillCollection.Add(objWorkingTimekeepersHoursCollection);
                    }
                }

                List<AllMonthData> lstAllMonthData = lstLastThreeAllMonthData.Where(a => a.FirstDateOfMonth >= StartDate && a.FirstDateOfMonth <= dEndDate).ToList();

                string empName = CommonClass.GetSplitString(EmployeeName);
                EmployeeStaffingFTECal objEmpFTE = lstEmpFTE.Where(m => m.EmployeeID == EmployeeID).FirstOrDefault();

                #region Bind Emp Details
                DateTime dStartDate = new DateTime(Year - 1, 12, 1);
                DateTime StartDateFromDecLastYear = new DateTime(Year - 1, 12, 1);

                //By alka-Date-06-08-2023---Here Getting Employee Salary detail using EmployeeCode
                List<CompensationSalaryIncrease> lstCSFilter = _commonClass.GetEmployeSalaryDetail(lstESDB, EmployeeCode.Trim());
                if (lstCSFilter.Count > 0)
                {
                    objCC.lstCompensationSalaryIncrease = lstCSFilter;
                }
                //CounselCompensationPlanningModel objCCP = new CounselCompensationPlanningModel();
                objCC.Name = EmployeeName;
                objCC.EmployeeCode = EmployeeCode;


                if (lstObjBillCollection.Count > 0)
                {
                    lstObjBillCollection = lstObjBillCollection.OrderByDescending(a => a.Year).ToList();
                    lstObjBillCollection.Where(a => a.Month == 12).ToList().ForEach(i =>
                    {
                        i.Year = i.Year + 1;
                    });
                    objCC.lstCompensationPerformanceBillColl = lstObjBillCollection.OrderBy(a => a.Year).ToList();
                }

                objCC.RoleCurrentYear = objCCDB?.IsNewEmployee == true ? objCCDB.NextAxinnRole : objEmpFTE.AxinnPosition?.Trim();
                objCC.NextAxinnRole = objCCDB?.IsNewEmployee == true ? objCCDB.NextAxinnRole : objEmpFTE.AxinnPosition?.Trim();
                objCC.LawSchoolGrad = objCCDB?.IsNewEmployee == true ? 0 : objEmpFTE.JDYear == "-" ? 0 : Convert.ToInt32(objEmpFTE.JDYear);
                objCC.HireDate = objCCDB?.IsNewEmployee == true ? objCCDB.HireDate : objEmpFTE.HireDate;
                if (objCC.HireDate > dStartDate)
                {
                    dStartDate = objCC.HireDate;
                }

                objCC.PracticeGroup = objCCDB?.IsNewEmployee == true ? objCCDB.PracticeGroup : objEmpFTE.DepartmentDesc;
                objCC.Office = objCCDB?.IsNewEmployee == true ? objCCDB.Office : objEmpFTE.OfficeDesc;
                objCC.AxinnYrCurrentYear = objCCDB?.IsNewEmployee == true ? 0 : objEmpFTE.ClassYear == "-" ? 0 : Convert.ToInt32(objEmpFTE.ClassYear);
                objCC.IsNewEmployee = objCCDB?.IsNewEmployee == true ? true : false;
                // objCCP.AxinnYrCurrentYear = objCPM.AxinnYear;
                //objCCP.Manager = objCPM.Manager;
                //objCCP.LawSchoolGrad = objCPM.LawSchollGrad; //JDYear
                // objCCP.IsCustomAdded = objCPM.IsCustomAdded;
                //objCCP.PositionChangeDate = objCPM.PositionChangeDate;

                decimal FeeCollection = lstAllMonthData.Where(m => m.EmployeeID == EmployeeID).Sum(m => m.FeeCollection);
                if (FeeCollection > 500000)
                    objCC.DiscretionaryBonusCollections = 2;

                //modified by Ramawatar Sharma
                //on 19/09/2023 
                //previous we were passing dEndDate , which value were previous month last date
                //now passing november month last date
                objCC.FTE = _commonClass.Calculate_FTE_Hiredate(objCC.HireDate, (int)GlobalVariable.CompForm.Counsel, lstELD, lstHolidays, new DateTime(Year, 11, 30), Year);
                objCC.NextYearFTE = 1;
                objCC.lstEmployeeLeaveDetails = _commonClass.GetEmployeeleaveDetails(lstELD, lstHolidays, dEndDate, Year);

                decimal BillableCredit = _commonClass.Get_BillableCredit("Counsel", lstAllMonthData.Where(m => m.EmployeeID == EmployeeID).ToList());
                objCC.BillableHours = lstAllMonthData.Where(m => m.EmployeeID == EmployeeID).Sum(m => m.ActualBaseHours);
                objCC.NonBillable = lstAllMonthData.Where(m => m.EmployeeID == EmployeeID && m.ClientName != "Pro Bono" && m.ClientName != "Axinn Professional Courtesy" && m.MatterName != "Diversity" && m.MatterName != "General Counsel" && m.MatterName != "Client Travel - NB" && m.MatterName != "Client Pitches" && m.MatterName != "General Marketing" && m.MatterName != "Client Alerts" && m.MatterName != "Articles, Books & Speeches" && m.MatterName != "Hiring" && m.MatterName != "Professional Development Leadership").Sum(item => item.ActualBaseHoursNB);
                objCC.ProBono = lstAllMonthData.Where(m => m.EmployeeID == EmployeeID && (m.ClientName == "Pro Bono" || m.ClientName == "Axinn Professional Courtesy")).Sum(item => item.ActualBaseHoursNB);
                objCC.DEI = lstAllMonthData.Where(m => m.EmployeeID == EmployeeID && m.MatterName == "Diversity").Sum(m => m.ActualBaseHoursNB);
                objCC.GeneralCounsel = lstAllMonthData.Where(a => a.EmployeeID == EmployeeID && a.MatterName == "General Counsel").Sum(a => a.ActualBaseHoursNB);
                objCC.TravelHours = lstAllMonthData.Where(a => a.EmployeeID == EmployeeID && a.MatterName == "Client Travel - NB").Sum(a => a.ActualBaseHoursNB);
                objCC.ProfessionalDeveloment = lstAllMonthData.Where(a => a.EmployeeID == EmployeeID && a.ClientName == "Axinn Nonbillable" && (a.MatterName == "Client Pitches" || a.MatterName == "General Marketing" || a.MatterName == "Client Alerts" || a.MatterName == "Articles, Books & Speeches" || a.MatterName == "Hiring" || a.MatterName == "Professional Development Leadership")).Sum(a => a.ActualBaseHoursNB);
                objCC.BillableCredit = (BillableCredit + objCC.BillableHours);
                objCC.YTDBillableHours = objCC.BillableHours;

                decimal AnnualizedHours = _commonClass.GetAnnualizedHours(objCC.BillableHours, lstELD, dStartDate, dEndDate, lstHolidays);
                AnnualizedHours = (AnnualizedHours + BillableCredit);
                objCC.annualHour = AnnualizedHours;
                objCC.FinalAdjustmentHrs = AnnualizedHours;

                if (objCC.annualHour > 2000)
                    objCC.DiscretionaryBonusBillables = 2;

                objCC.BonusesCurrentYear = objCC.BonusesCurrentYear;
                objCC.CorrectedBillableHours = 0;
                objCC.ProjectedNextYearBeforeDiscretionaryBonus = 0;
                objCC.PaidBonusHours = lstExpensesData.Where(m => m.EmployeeID == EmployeeID && m.adpPayType == "HoursBonus").Sum(m => m.actualYTD);

                if (objCCDB != null)
                {
                    objCC.ID = objCCDB.ID;
                    objCC.DiscretionaryBonusCollections = objCCDB.DiscretionaryBonusCollections;
                    objCC.DiscretionaryBonusBillables = objCCDB.DiscretionaryBonusBillables;
                    objCC.AssocReferralProgram = objCCDB.AssocReferralProgram;
                    objCC.NextAxinnRole = objCCDB.NextAxinnRole;
                    objCC.NextAxinnBase = objCCDB.NextAxinnBase;
                    objCC.PGChairComments = objCCDB.PGChairComments;
                    objCC.NextYearFTE = objCCDB.FTE;
                    objCC.Adjustment = objCCDB.Adjustment;
                    objCC.AdjustmentComments = objCCDB.AdjustmentComments;
                    objCC.NextYearAdj = objCCDB.NextYearAdj;
                    objCC.NextYearAdjComment = objCCDB.NextYearAdjComment;
                    objCC.SpecialBonus = objCCDB.SpecialBonus;
                    objCC.Compensation = objCCDB.Compensation;
                    objCC.CurrentYearYEScore = objCCDB.CurrentYearYEScore;
                    objCC.CurrentYearMidYearScore = objCCDB.CurrentYearMidYearScore;

                    objCC.AdjustmentHrs = objCCDB.AdjustmentHrs;
                    objCC.AdjustmentHrsComments = objCCDB.AdjustmentHrsComments;
                    objCC.IsAdjustedHrsPercentage = objCCDB.IsAdjustedHrsPercentage;
                    objCC.IsAdjusted_YearEnd_Percentage = objCCDB.IsAdjusted_YearEnd_Percentage;

                    if (objCCDB.IsAdjustedHrsPercentage == true)
                    {
                        objCC.FinalAdjustmentHrs = (objCC.FinalAdjustmentHrs * objCC.AdjustmentHrs) / 100;
                    }
                    else
                    {
                        objCC.FinalAdjustmentHrs = objCC.FinalAdjustmentHrs + objCC.AdjustmentHrs;
                    }

                    var BonusOtherExtraAdded = 0.0M;
                    foreach (Compensation_Extra_Column objCE in lstCECDB)
                    {
                        if (objCE.ColumnName == "Column1")
                        {
                            objCC.Column1 = objCCDB.Column1;
                        }
                        else if (objCE.ColumnName == "Column2")
                        {
                            objCC.Column2 = objCCDB.Column2;
                        }
                        else if (objCE.ColumnName == "Column3")
                        {
                            objCC.Column3 = objCCDB.Column3;
                        }
                        else if (objCE.ColumnName == "Column4")
                        {
                            objCC.Column4 = objCCDB.Column4;
                        }
                        else if (objCE.ColumnName == "Column5")
                        {
                            objCC.Column5 = objCCDB.Column5;
                        }
                        else if (objCE.ColumnName == "Column6")
                        {
                            objCC.Column6 = objCCDB.Column6;
                            BonusOtherExtraAdded += objCCDB.Column6;
                        }
                        else if (objCE.ColumnName == "Column7")
                        {
                            objCC.Column7 = objCCDB.Column7;
                            BonusOtherExtraAdded += objCCDB.Column7;
                        }
                        else if (objCE.ColumnName == "Column8")
                        {
                            objCC.Column8 = objCCDB.Column8;
                            BonusOtherExtraAdded += objCCDB.Column8;
                        }
                        else if (objCE.ColumnName == "Column9")
                        {
                            objCC.Column9 = objCCDB.Column9;
                            BonusOtherExtraAdded += objCCDB.Column9;
                        }
                        else if (objCE.ColumnName == "Column10")
                        {
                            objCC.Column10 = objCCDB.Column10;
                            BonusOtherExtraAdded += objCCDB.Column10;
                        }
                    }
                    objCC.BonusOtherExtraAdded = BonusOtherExtraAdded;
                }

                if (dtPenality == null)
                {
                    if (DateTime.Now.Year != Year)
                    {
                        objCC.YearPenalty = _commonClass.GetPenalityAmount(EmployeeName, new DateTime(Year, 12, 31).ToString());
                    }
                    else
                    {
                        objCC.YearPenalty = _commonClass.GetPenalityAmount(EmployeeName, DateTime.Now.ToString());
                    }
                }
                else
                {
                    objCC.YearPenalty = Convert.ToInt32(dtPenality.Select("EmployeeCode='" + EmployeeCode + "'").Count() > 0 ? dtPenality.Select("EmployeeCode='" + EmployeeCode + "'")[0]["YearPenalty"] : "0");
                }

                if (isForCompensationSummary == false)
                {
                    objCC.lstCompensationPerformanceScore = _commonClass.GetPerformanceScore_Compensation(EmployeeName, dtPerformanceScore);
                    objCC.lstCompensationPerformanceScore = BindYEScoreFromDB(EmployeeCode.Trim(), objCC.lstCompensationPerformanceScore, lstCounselYEScore, Year);
                }

            }
            #endregion

            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                objCC = null;
            }
            return objCC;
        }

        public List<TimekeeperCompensationPerformanceScoreModel> BindYEScoreFromDB(string EmployeeCode, List<TimekeeperCompensationPerformanceScoreModel> lstCompensationPerformanceScore, List<CounselCompensationPlanningModel> lstCounselYEScore, int Year)
        {
            try
            {
                lstCompensationPerformanceScore = lstCompensationPerformanceScore.Where(a => a.Year >= (Year - 3) && a.Year < Year).ToList();

                for (int YSYear = (Year - 3); YSYear < Year; YSYear++)
                {
                    if (lstCompensationPerformanceScore.FindAll(m => m.Year == YSYear && m.YearYE == 0 && m.MidYearYE == 0).Count > 0)
                    {
                        CounselCompensationPlanningModel objCompYEScore = lstCounselYEScore.Find(m => m.EmployeeCode == EmployeeCode && m.Year == YSYear);
                        if (objCompYEScore != null)
                        {
                            TimekeeperCompensationPerformanceScoreModel objYEScor = lstCompensationPerformanceScore.Find(m => m.Year == YSYear);
                            if (objYEScor != null)
                            {
                                objYEScor.YearYE = objCompYEScore.CurrentYearYEScore;
                                objYEScor.MidYearYE = objCompYEScore.CurrentYearMidYearScore;
                            }
                        }
                    }
                }
                return lstCompensationPerformanceScore;
            }
            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                return null;
            }
        }

        public List<CounselCompensationHistory> GetCounselHistoryDetails()
        {
            CounselCompensationHistory objCCH = new CounselCompensationHistory();
            List<CounselCompensationHistory> lstCCH = new List<CounselCompensationHistory>();
            try
            {
                //string fileName = "Counsel Comp_2018-2021.xlsx";
                //string path = HttpContext.Current.Server.MapPath("~/Assets/Files/Excel/");
                //DataTable dtCounselHistory = excelFile.ReadFile(fileName, path);
                DataTable dtCounselHistory = _service.DAGetDataSet("EXEC[usp_Compensation_Partner_Comp] @Action ='Get', @Criteria ='Get_Compensation_Partner_Excel_Data'").Tables[0];
                if (dtCounselHistory != null && dtCounselHistory.Rows.Count > 0)
                {
                    for (int i = 0; i < dtCounselHistory.Rows.Count; i++)
                    {
                        objCCH = new CounselCompensationHistory();
                        var objCCHTD = new CounselCompensationHistoryYearDetails();
                        List<CounselCompensationHistoryYearDetails> lstCCHTD = new List<CounselCompensationHistoryYearDetails>();
                        objCCH.Name = dtCounselHistory.Rows[i]["EmployeeName"].ToString();
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["EmployeeHireDate"].ToString()))
                        {
                            objCCH.HireDate = Convert.ToDateTime(dtCounselHistory.Rows[i]["EmployeeHireDate"]);
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["StatusChangeDate"].ToString()))
                        {
                            objCCH.StatusChangeDate = Convert.ToDateTime(dtCounselHistory.Rows[i]["StatusChangeDate"]);
                        }
                        objCCH.StatusChange = dtCounselHistory.Rows[i]["Status"].ToString();

                        objCCHTD.Year = 2018;
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2018_Base"].ToString()))
                        {
                            objCCHTD.Base = Convert.ToDecimal(dtCounselHistory.Rows[i]["2018_Base"].ToString().Replace("$", ""));
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2018_Bonuses"].ToString()))
                        {
                            objCCHTD.Bonuses = Convert.ToDecimal(dtCounselHistory.Rows[i]["2018_Bonuses"].ToString().Replace("$", ""));
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2018_Total"].ToString()))
                        {
                            objCCHTD.Total = Convert.ToDecimal(dtCounselHistory.Rows[i]["2018_Total"].ToString().Replace("$", ""));
                        }

                        lstCCHTD.Add(objCCHTD);

                        objCCHTD = new CounselCompensationHistoryYearDetails();
                        objCCHTD.Year = 2019;
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2019_Base"].ToString()))
                        {
                            objCCHTD.Base = Convert.ToDecimal(dtCounselHistory.Rows[i]["2019_Base"].ToString());
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2019_Bonuses"].ToString()))
                        {
                            objCCHTD.Bonuses = Convert.ToDecimal(dtCounselHistory.Rows[i]["2019_Bonuses"].ToString());
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2019_Total"].ToString()))
                        {
                            objCCHTD.Total = Convert.ToDecimal(dtCounselHistory.Rows[i]["2019_Total"].ToString());
                        }

                        lstCCHTD.Add(objCCHTD);

                        objCCHTD = new CounselCompensationHistoryYearDetails();
                        objCCHTD.Year = 2020;
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2020_Base"].ToString()))
                        {
                            objCCHTD.Base = Convert.ToDecimal(dtCounselHistory.Rows[i]["2020_Base"].ToString());
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2020_Bonuses"].ToString()))
                        {
                            objCCHTD.Bonuses = Convert.ToDecimal(dtCounselHistory.Rows[i]["2020_Bonuses"].ToString());
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2020_Total"].ToString()))
                        {
                            objCCHTD.Total = Convert.ToDecimal(dtCounselHistory.Rows[i]["2020_Total"].ToString());
                        }

                        lstCCHTD.Add(objCCHTD);

                        objCCHTD = new CounselCompensationHistoryYearDetails();
                        objCCHTD.Year = 2021;
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2021_Base"].ToString()))
                        {
                            objCCHTD.Base = Convert.ToDecimal(dtCounselHistory.Rows[i]["2021_Base"].ToString());
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2021_Bonuses"].ToString()))
                        {
                            objCCHTD.Bonuses = Convert.ToDecimal(dtCounselHistory.Rows[i]["2021_Bonuses"].ToString());
                        }
                        if (!string.IsNullOrEmpty(dtCounselHistory.Rows[i]["2021_Total"].ToString()))
                        {
                            objCCHTD.Total = Convert.ToDecimal(dtCounselHistory.Rows[i]["2021_Total"].ToString());
                        }

                        lstCCHTD.Add(objCCHTD);

                        objCCH.lstCounselCompensationHistoryYearDetails = lstCCHTD;

                        lstCCH.Add(objCCH);
                    }
                }
                lstCCH.Where(a => a.StatusChange == "Counsel").ToList().ForEach(i =>
                {
                    i.Type = "Counsel";
                });

                lstCCH.Where(a => a.StatusChange == "Termed").ToList().ForEach(i =>
                {
                    i.Type = "Termed";
                });
            }
            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
            }
            return lstCCH;
        }

        public int Insert_Update_Counsel_Compensation_Trans(string CCDetails, string sUserID)
        {
            try
            {
                int Result = 0;
                List<CounselCompensationPlanningModel> lstCC = JsonConvert.DeserializeObject<List<CounselCompensationPlanningModel>>(CCDetails);
                using (TransactionScope tranScope = new TransactionScope())
                {
                    try
                    {
                        Result = Insert_Update_Counsel_Compensation(lstCC, sUserID);
                        if (Result < 0)
                        {
                            tranScope.Dispose();
                            return -1;
                        }

                        tranScope.Complete();
                        return Result;
                    }
                    catch (Exception ex)
                    {
                        tranScope.Dispose();
                        _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                        return -1;
                    }
                }
            }
            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                return -1;
            }
        }

        public int Insert_Update_Counsel_Compensation(List<CounselCompensationPlanningModel> lstTC, string UserID)
        {
            int iResult = 0;
            try
            {
                #region start Bulk Insert

                List<CounselCompensationPlanningModel> lstCCDB = lstTC.FindAll(m => m.ID > 0);

                List<CounselCompensationPlanningModel> lstCCWithOutID = lstTC.FindAll(m => m.ID <= 0);

                string strXdoc = string.Empty;
                string strXdocDB = string.Empty;
                string empstrXdoc = string.Empty;
                string empstrXdocDB = string.Empty;
                if (lstCCWithOutID.Count > 0)
                {

                    XElement Xdoc = new XElement("CCForm",
                        from lst in lstCCWithOutID
                        select new XElement("FieldInfo",
                        new XElement("ID", lst.ID),
                        new XElement("Name", lst.Name.Trim()),
                        new XElement("EmployeeCode", lst.EmployeeCode.Trim()),
                        new XElement("DiscretionaryBonusCollections", lst.DiscretionaryBonusCollections),
                        new XElement("DiscretionaryBonusBillables", lst.DiscretionaryBonusBillables),
                        new XElement("AssocReferralProgram", lst.AssocReferralProgram),
                        new XElement("NextAxinnRole", lst.NextAxinnRole),
                        new XElement("NextAxinnBase", lst.NextAxinnBase),
                        new XElement("PGChairComments", lst.PGChairComments),
                        new XElement("FTE", lst.FTE),
                        new XElement("Adjustment", lst.Adjustment),
                        new XElement("AdjustmentComments", lst.AdjustmentComments),
                         new XElement("NextYearAdj", lst.NextYearAdj),
                        new XElement("NextYearAdjComment", lst.NextYearAdjComment),
                        new XElement("SpecialBonus", lst.SpecialBonus),
                        new XElement("Compensation", lst.Compensation),
                        new XElement("CurrentYearMidYearScore", lst.CurrentYearMidYearScore),
                        new XElement("CurrentYearYEScore", lst.CurrentYearYEScore),

                        new XElement("InsertedBy", UserID),
                         new XElement("Column1", lst.Column1),
                         new XElement("Column2", lst.Column2),
                         new XElement("Column3", lst.Column3),
                         new XElement("Column4", lst.Column4),
                         new XElement("Column5", lst.Column5),
                         new XElement("Column6", lst.Column6),
                         new XElement("Column7", lst.Column7),
                         new XElement("Column8", lst.Column8),
                         new XElement("Column9", lst.Column9),
                         new XElement("Column10", lst.Column10),
                         new XElement("Year", lst.Year),
                         new XElement("AdjustedHours", lst.AdjustmentHrs),
                         new XElement("AdjustedHoursComments", lst.AdjustmentHrsComments),
                         new XElement("IsAdjustedHrsPercentage", lst.IsAdjustedHrsPercentage),
                         new XElement("IsAdjusted_YearEnd_Percentage", lst.IsAdjusted_YearEnd_Percentage),
                         new XElement("IsNewEmployee", lst.IsNewEmployee)

                        ));
                    strXdoc = Xdoc.ToString();
                    strXdoc = strXdoc.Replace("'", "&apos;");

                    lstCCWithOutID = lstCCWithOutID.FindAll(s => s.IsNewEmployee == true);

                    if (lstCCWithOutID.Count > 0)
                    {
                        XElement xDocs = new XElement("CNCForm",
                       from lst in lstCCWithOutID
                       select new XElement("FieldInfo",
                       new XElement("Compensation_ID", lst.ID),
                       new XElement("EmployeeName", lst.Name.Trim()),
                       new XElement("PracticeGroup", lst.PracticeGroup),
                       new XElement("Office", lst.Office),
                       new XElement("JDYear", lst.JDYear),
                       new XElement("HireDate", lst.HireDate),
                       new XElement("MangerName", lst.ManagerName),
                       new XElement("NextAnnualCompensation", lst.AnnualCompensation.HasValue ? lst.AnnualCompensation.Value.ToString("F2") : "0.00"),
                       new XElement("LU_Compensation_Form_ID", Convert.ToInt32(GlobalVariable.CompForm.Counsel)),
                       new XElement("Year", lst.Year),
                       new XElement("CreatedBy", UserID)
                       ));
                        empstrXdoc = xDocs.ToString();
                    }
                }

                if (lstCCDB.Count > 0)
                {
                    XElement Xdoc = new XElement("CCForm",
                        from lst in lstCCDB
                        select new XElement("FieldInfo",
                        new XElement("ID", lst.ID),
                        new XElement("Name", lst.Name.Trim()),
                        new XElement("EmployeeCode", lst.EmployeeCode.Trim()),
                        new XElement("DiscretionaryBonusCollections", lst.DiscretionaryBonusCollections),
                        new XElement("DiscretionaryBonusBillables", lst.DiscretionaryBonusBillables),
                        new XElement("AssocReferralProgram", lst.AssocReferralProgram),
                        new XElement("NextAxinnRole", lst.NextAxinnRole),
                        new XElement("NextAxinnBase", lst.NextAxinnBase),
                        new XElement("PGChairComments", lst.PGChairComments),
                        new XElement("FTE", lst.FTE),
                        new XElement("Adjustment", lst.Adjustment),
                        new XElement("AdjustmentComments", lst.AdjustmentComments),
                         new XElement("NextYearAdj", lst.NextYearAdj),
                        new XElement("NextYearAdjComment", lst.NextYearAdjComment),
                        new XElement("SpecialBonus", lst.SpecialBonus),
                        new XElement("Compensation", lst.Compensation),
                         new XElement("CurrentYearMidYearScore", lst.CurrentYearMidYearScore),
                        new XElement("CurrentYearYEScore", lst.CurrentYearYEScore),

                        new XElement("InsertedBy", UserID),
                         new XElement("Column1", lst.Column1),
                         new XElement("Column2", lst.Column2),
                         new XElement("Column3", lst.Column3),
                         new XElement("Column4", lst.Column4),
                         new XElement("Column5", lst.Column5),
                         new XElement("Column6", lst.Column6),
                         new XElement("Column7", lst.Column7),
                         new XElement("Column8", lst.Column8),
                         new XElement("Column9", lst.Column9),
                         new XElement("Column10", lst.Column10),
                         new XElement("Year", lst.Year),

                         new XElement("AdjustedHours", lst.AdjustmentHrs),
                         new XElement("AdjustedHoursComments", lst.AdjustmentHrsComments),
                         new XElement("IsAdjustedHrsPercentage", lst.IsAdjustedHrsPercentage),
                         new XElement("IsAdjusted_YearEnd_Percentage", lst.IsAdjusted_YearEnd_Percentage)
                        ));
                    strXdocDB = Xdoc.ToString();

                    strXdocDB = strXdocDB.Replace("'", "&apos;");

                    lstCCDB = lstCCDB.FindAll(s => s.IsNewEmployee == true);
                    if (lstCCDB.Count > 0)
                    {
                        XElement xDocs = new XElement("CNCForm",
                        from lst in lstCCDB
                        select new XElement("FieldInfo",
                        new XElement("Compensation_ID", lst.ID),
                        new XElement("EmployeeName", lst.Name.Trim()),
                        new XElement("PracticeGroup", lst.PracticeGroup),
                        new XElement("Office", lst.Office),
                        new XElement("JDYear", lst.JDYear),
                        new XElement("HireDate", lst.HireDate),
                        new XElement("MangerName", lst.ManagerName),
                        new XElement("NextAnnualCompensation", lst.AnnualCompensation.HasValue ? lst.AnnualCompensation.Value.ToString("F2") : "0.00"),
                        new XElement("LU_Compensation_Form_ID", Convert.ToInt32(GlobalVariable.CompForm.Counsel)),
                        new XElement("Year", lst.Year),
                        new XElement("CreatedBy", UserID)
                        ));
                        empstrXdocDB = xDocs.ToString();
                    }
                }

                iResult = Convert.ToInt32(_service.SaveBulkData_SC("usp_Compensation_Counsel_XML", Convert.ToString(strXdoc), Convert.ToString(strXdocDB)));
                if (iResult < 0)
                {
                    return -1;
                }

                if (empstrXdoc != "" || empstrXdocDB != "")
                {
                    iResult = Convert.ToInt32(_service.SaveBulkData_SC("usp_Compensation_New_Employee_XML", Convert.ToString(empstrXdoc), Convert.ToString(empstrXdocDB)));

                    if (iResult < 0)
                    {
                        return -1;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                return -1;
            }

            return iResult;
        }

        public List<CounselCompensationPlanningModel> GetManagerDetails(List<CounselCompensationPlanningModel> lstPC, List<CounselCompensationPlanningModel> lstCPlogInUser, List<CounselCompensationPlanningModel> lstFinalManagerDetails)
        {
            List<CounselCompensationPlanningModel> lstManagerDetails = new List<CounselCompensationPlanningModel>();

            foreach (CounselCompensationPlanningModel objCP in lstCPlogInUser)
            {
                List<CounselCompensationPlanningModel> lstCPManagerDetails = lstPC.FindAll(m => m.Manager.Replace(",", "").Replace(".", "").Replace("'", "") == objCP.Name.Replace(",", "").Replace(".", "").Replace("'", ""));

                if (lstCPManagerDetails.Count > 0)
                {
                    lstManagerDetails.AddRange(lstCPManagerDetails);

                    foreach (CounselCompensationPlanningModel obj in lstCPManagerDetails)
                    {
                        List<CounselCompensationPlanningModel> lst = lstPC.FindAll(m => m.Manager == obj.Name);
                        if (lst.Count > 0)
                        {
                            List<CounselCompensationPlanningModel> lstCPDetails = GetManagerDetails(lstPC, lst, lstFinalManagerDetails);
                            lstManagerDetails.AddRange(lstCPManagerDetails);
                        }
                    }
                }
            }

            return lstManagerDetails;
        }

        public List<CounselCompensationPlanningModel> FinalGetManagerDetails(List<CounselCompensationPlanningModel> lstCP)
        {
            List<CounselCompensationPlanningModel> lstFinalManagerDetails = new List<CounselCompensationPlanningModel>();

            List<QlikUser> lstManager = new List<QlikUser>();

            DataTable dtManager = _service.DAGetDataSet("EXEC[usp_Compensation_Class_Year] @Action ='Get', @Criteria ='Get_ADPName'").Tables[0];
            //Qlik_UserName, ADPName
            if (dtManager != null && dtManager.Rows.Count > 0)
            {
                lstManager = CommonClass.ConvertDataTable<QlikUser>(dtManager);
            }

            foreach (CounselCompensationPlanningModel objCPManager in lstCP)
            {
                QlikUser objUser = lstManager.Find(m => m.ADPName == objCPManager.Manager);
                if (objUser != null)
                {
                    objCPManager.Manager = objUser.Qlik_UserName;
                }
            }

            List<CounselCompensationPlanningModel> lstCPlogInUser = lstCP.FindAll(m => m.Manager == "Aggarwal, Neeraj K.");

            if (lstCPlogInUser.Count > 0)
            {
                lstFinalManagerDetails.AddRange(lstCPlogInUser);

                List<CounselCompensationPlanningModel> lstCPManager = GetManagerDetails(lstCP, lstCPlogInUser, lstFinalManagerDetails);
                if (lstCPManager.Count > 0)
                {
                    lstFinalManagerDetails.AddRange(lstCPManager);
                }
            }

            return lstFinalManagerDetails;
        }
        public int Delete_Counsel_Compensation(int ID)
        {
            try
            {
                int Result = 0;
                using (TransactionScope tranScope = new TransactionScope())
                {
                    try
                    {

                        Result = _service.ExecuteNonQuery_AllowedTransaction("EXEC[usp_Compensation_Counsel] @Action ='Delete',@ID='" + ID + "'");
                        if (Result < 0)
                        {

                            tranScope.Dispose();
                            return -1;

                        }
                        Result = _service.ExecuteNonQuery_AllowedTransaction("EXEC[usp_Compensation_New_Employee] @Action ='Delete',@ID='" + ID + "',@LU_Comp_Form_ID='" + (int)GlobalVariable.CompForm.Counsel + "'");
                        if (Result < 0)

                        {
                            tranScope.Dispose();
                            return -1;

                        }
                        tranScope.Complete();
                        return Result;

                    }

                    catch (Exception ex)
                    {

                        tranScope.Dispose();
                        _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                        return -1;
                    }
                }
            }

            catch (Exception ex)
            {
                _commonClass.LogErrorToFile(_commonClass.GetCurrentPageName(), ex.Message.Trim() + Environment.NewLine + ex.StackTrace.Trim());
                return -1;
            }
        }

    }
}
