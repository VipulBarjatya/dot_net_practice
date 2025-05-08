var flag = false;
var fteFlag = false;

// Local storage key for saving compensation data
const STORAGE_KEY = 'counselCompensationData';
const LAST_SAVED_KEY = 'counselCompensationLastSaved';

var ccp = {

    init: function () {

        // Add to the init function in ccp object
        $('#tblCounselCompensationPlanning').on('focusout', 'input[type=text], textarea, select', function (e) {
            // Don't autosave when focusing between inputs in the same row
            if ($(e.relatedTarget).closest('tr').is($(this).closest('tr'))) {
                return;
            }

            // Mark the row as updated
            $(this).closest('tr').find('.hdnIsUpdated').val('true');
            flag = true;

            // Save data to local storage
            ccp.saveToLocalStorage();
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtSpecialBonus', function (e) {
            ccp.setSubTotalBonusNFinalCompensation(this, 'Decimal');
            ccp.manageHeaderTotal();
        });

        $('#tblCounselCompensationPlanning').on('keypress', '.number', function (e) {

            let isNegative = $(this).hasClass('txtcompensation');
            if (isNegative == true) {
                return allowNumericValueSelection(this, e, $(this).hasClass('txtcompensation'), 9);
            } else {
                return allowNumericValueSelection(this, e, $(this).hasClass('extracolumn'), 9);
            }
        });

        $('#tblCounselCompensationPlanning').on('keypress', '.numberwithdecimal', function (event) {

            return allowFixedNumericValBefAftDecimal(this, event, txtlen = 3);
        });

        $('#tblCounselCompensationPlanning').on('keypress', '.numberwithFTEdecimal', function (event) {
            return allowFixedNumericValBefAftDecimal(this, event, txtlen = 1);
        });

        $('#tblCounselCompensationPlanning').on('keypress', '.numberwithNegativeFTEdecimal', function (event) {
            return allowNegativeFixedNumericValBefAftDecimal(this, event, 1, 2);
        });

        $('#tblCounselCompensationPlanning').on('change', '.number', function (e) {
            var values = common.numberWithCommas($(this).val().replace(/,/g, ''));
            $(this).val(values);
            //setFormatValueWithOutNegative(this);
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtNextYearFTE', function (e) {
            flag = true;
            $(this).closest('tr').find('.hdnIsUpdated').val('true');
            if ($(this).hasClass('numberwithNegativeFTEdecimal')) {
                common.setNegativeFormatValue(this, true);
            } else {
                common.setFormatValue(this, true);
            }
            var values = $(this).val() == "" ? 0.00 : $(this).val().replace(/,/g, '');

            var $tr = $(this).closest('tr');
            if (parseFloat(values) > 1) {
                fteFlag = true;
                $tr.find('.dvErrMsgFTE').text('FTE cannot be more than 1.00');
                $tr.find('.txtNextYearFTE').css('border-color', 'red');
                return false;
            } else if (parseFloat(values) < -1) {
                fteFlag = true;
                $tr.find('.dvErrMsgFTE').text('FTE cannot be less than -1.00');
                $tr.find('.txtNextYearFTE').css('border-color', 'red');
                return false;
            }
            else {
                fteFlag = false;
                $tr.find('.dvErrMsgFTE').text('');
                $tr.find('.hdnNextYearFTE').val($(this).val());
                //$tr.find('.CompensationNextYear').each(function () {
                //    var CompensationNextYear = $(this).attr('data-attr');
                //    var Compensation = (CompensationNextYear * values);
                //    $(this).text(common.numberWithCommasDash(Compensation.toFixed(0)));
                //});
                //ccp.setAdjCompensation(this);
            }
            ccp.manageHeaderTotal();
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtAdjustment', function (e) {
            try {
                ccp.setSubTotalBonusNFinalCompensation(this, 'Decimal', 'AdjYEBonus', 'allowMinus');
                ccp.manageHeaderTotal();
                //commented by Ramawatar Sharma
                //on 19 Jul, 2023
                //formatting value of this has been moved to compensation common js
                //ccp.setFormatValue(this);
            }
            catch (err) {
                console.log(err);
            }
        });

        $('#tblCounselCompensationPlanning .colexpend').click(function (e) {
            try {
                e.stopImmediatePropagation();

                $('#tblCounselCompensationPlanning .tdBillableCreditcallasped').css('display', 'table-cell')
                $('#tblCounselCompensationPlanning .thBillableCredit').css('display', 'table-cell');
                var currentYearHeaderMainColspan = parseInt($('.currentYearHeaderMain').attr("colspan")) + 7;
                $('.currentYearHeaderMain').attr("colspan", currentYearHeaderMainColspan);

                $('#tblCounselCompensationPlanning .thBillableCreditExpend').css('display', 'none')
                $('#tblCounselCompensationPlanning .tdBillableCreditcallExpend').css('display', 'none');

                $('[data-toggle="tooltip"]').tooltip({
                    container: 'body'
                });

                ccp.manageCounselCompensationPlanningFixedHeader();

                e.stopPropagation();
            }
            catch (err) {
                console.log(err);
            }
        });

        $('#tblCounselCompensationPlanning').on('click', '.colasped', function (e) {
            try {
                e.stopImmediatePropagation();

                $('#tblCounselCompensationPlanning .tdBillableCreditcallasped').css('display', 'none')
                $('#tblCounselCompensationPlanning .thBillableCredit').css('display', 'none');
                var currentYearHeaderMainColspan = parseInt($('.currentYearHeaderMain').attr("colspan")) - 7;
                $('.currentYearHeaderMain').attr("colspan", currentYearHeaderMainColspan);

                $('#tblCounselCompensationPlanning .thBillableCreditExpend').css('display', 'table-cell')
                $('#tblCounselCompensationPlanning .tdBillableCreditcallExpend').css('display', 'table-cell');

                ccp.manageCounselCompensationPlanningFixedHeader();

            } catch (err) {
                console.log(err);
            }
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtNextYearAdj', function (e) {
            try {
                ccp.setFormatValueWithNegtive(this);
                ccp.setAdjCompensation(this);
            }
            catch (err) {
                console.log(err);
            }
        });

        $('#tblCounselCompensationPlanning').on('change', '.extracolumn', function (e) {
            var dataType = $(this).attr('data-attr').toLowerCase();
            ccp.setSubTotalBonusNFinalCompensation(this, dataType, '', 'allowMinus');
            ccp.manageHeaderTotal();
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtcompensation', function (e) {
            //setFormatValueWithOutNegative(this);
            $(this).closest('tr').find('.hdnCompensation').val($(this).val().replace(/,/g, ''));
            ccp.manageHeaderTotal();
            commonCompensation.calculateBudgetSalary($(this).closest('tr'));
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtCurrentYearMidYearScore', function (e) {
            ccp.setFormatValue(this);
        });

        $('#tblCounselCompensationPlanning').on('change', '.txtCurrentYearYEScore', function (e) {
            ccp.setFormatValue(this);
        });

        $('#tblCounselCompensationPlanning').on('change', '.ddlNextAxinnRole', function (e) {
            try {
                e.stopImmediatePropagation();
                var $tr = $(this).closest('tr');
                if ($(this).val() == 'Counsel') {
                    if ($tr.find('.hdnNextYearFTE').val() == "0.00") {
                        $tr.find('.txtNextYearFTE').val($tr.find('.hdnNextYearFTE').val("1.00")).prop('disabled', false);
                    }
                    $tr.find('.txtNextYearFTE').val($tr.find('.hdnNextYearFTE').val()).prop('disabled', false);
                    $tr.find('.txtcompensation').val(common.numberWithCommas(parseFloat($tr.find('.hdnCompensation').val()).toFixed(0))).prop('disabled', false);
                }
                else {
                    $tr.find('.txtNextYearFTE').val('0.00').prop('disabled', true);
                    $tr.find('.txtcompensation').val('-').prop('disabled', true);
                }
            }
            catch (err) {
                console.log(err);
            }
        });

        $('#tblCounselCompensationPlanning input[type=text], textarea').on('keyup', function (e) {
            $(this).closest('tr').find('.hdnIsUpdated').val('true');
            flag = true;
        });

        $('#tblCounselCompensationPlanning').on('focus', 'input[type=text]', function () {
            if ($(this).val() == '-') {
                $(this).val('');
            }
        })

        $("select").change(function () {
            $(this).closest('tr').find('.hdnIsUpdated').val('true');
            flag = true;
        })

        $('.cancelCP').click(function () {
            if (flag) {
                Swal.fire({
                    text: 'You have unsaved changes! Do you want to proceed?',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: "Yes",
                    cancelButtonText: "No",
                    closeOnConfirm: false,
                    closeOnCancel: true
                }).then((result) => {
                    if (result['isConfirmed']) {
                        window.location.href = window.location.href;
                    }
                })
            }
            else {
                window.location.href = window.location.href;
            }
        });

        $('.submitCCP').click(function () {
            ccp.saveCounselCompensationPlanning()
                .then(function (response) {
                    if (response.status == 200) {
                    ccp.clearLocalStorage();
                    }
                    else {
                        Swal.fire("", "Internal server error...", "error");
                    }
                })
                .catch(function (error) {
                    console.log(error)
                });
        });

        $('.submitExtraColumn').click(function () {
            if (ccp.IsExtraColumnValidate() == true) {
                var updatedCount = 0;
                $('.tblCompensationCommon').find('.hdnIsUpdated').each(function () {
                    if ($(this).val() == true) {
                        updatedCount += 1;
                    }
                });

                if ($('.tblCompensationCommon').find('.newRow').length > 0 && updatedCount > 0) {
                    flag = true;
                }
                ccp.CheckDuplicateExtraColumnName();
            }
        });

        $('.CloseExtraColumnPopUp').click(function () {
            $('.txtcolumnname').val('');
        });

        $('.AddNewColumn').click(function (e) {
            e.stopImmediatePropagation();
            var isYearEnd = $(this).closest('th').attr('data-isYearEnd');
            if (isYearEnd == '1')
                $('.hdnIsYearEnd').val(true);
            else
                $('.hdnIsYearEnd').val(false);

            $('.dvcolumnname').text('');
            $('.txtcolumnname').val('');
            $('.datatype').prop('selectedIndex', 0);
            $('.hdnID').val('0');
            $('.AddColumnHeader').text('Add New Column');
            $('#dvAddNewColumn .modal-footer').css('display', 'none');
            $("#dvAddNewColumn").modal("show");
            e.stopPropagation();
        });

        $('.EditColumn').click(function (e) {
            e.stopImmediatePropagation();
            var ID = $(this).attr('data-id');
            var ColumnName = $(this).attr('data-attr');
            var DataType = $(this).attr('data-datatype');
            $('.dvcolumnname').text('');
            $('.txtcolumnname').val(ColumnName);
            $('.datatype').val(DataType);
            $('.hdnID').val(ID);
            $('.AddColumnHeader').text('Edit Column');
            $('.modal-footer').css('display', 'block');
            $('.dvcolumnname').text('');
            $("#dvAddNewColumn").modal("show");
            $('.dvcolumnname').text('');
            e.stopPropagation();
        });

        $('.removeExtraColumn').click(function () {
            if ($('.tblCompensationCommon').find('.newRow').length > 0) {
                flag = true;
            }
            $("#dvConfirmation").modal("show");
        });

        $('.confirmYes').click(function () {
            if (flag) {
                $('#hdnDeleteColumnUnsavedChangesYes').val(1);
                $("#dvUnsavedChangesConfirmation").modal("show");
            }
            else {
                $('#hdnDeleteColumnUnsavedChangesYes').val(0);
                ccp.inactiveExtraColumn();
                $("#dvConfirmation").modal("none");
                $("#dvAddNewColumn").modal("none");
            }
        });

        $('.confirmUnsavedChangesYes').click(function () {
            ccp.saveCounselCompensationPlanning();
        });

        $('.confirmUnsavedChangesNo').click(function () {
            $('#hdnDeleteColumnUnsavedChangesYes').val(0);
            $('.txtcolumnname').val('');
            $("#dvUnsavedChangesConfirmation").modal("hide");
            $("#dvConfirmation").modal("hide");
            $("#dvAddNewColumn").modal("hide");
        });

        $('.tabParent').on('click', '.tab', function (e) {
            if ($(this).hasClass("freezedTab") == false) {
                $('.CompensationFreezeDetailsWrapper').addClass('hide');

                $('.tabParent .tab').removeClass("tab-selected");
                $(this).addClass("tab-selected");
                if ($(this).attr("data-tab-id") == "1") {
                    $('<form/>', { action: gWebsiteURL + 'report/counsel-compensation-planning', method: 'POST' }).append(
                        $('<input>', { type: 'hidden', id: 'Year', name: 'Year', value: $(this).attr('data-year') })
                    ).appendTo('body').submit();
                }
                else if ($(this).attr("data-tab-id") == "2") {
                    $('.divInnerPopup').css('display', 'flex');
                    $('#divBonusGrid').removeClass('hide');
                    $('#divBonusGrid').closest('.sub-tabs').removeClass("hide");
                    $('#divCompensationGrid').addClass("hide");
                    $('#divBonusGrid').html("");
                    $('#divHistoryGrid').addClass("hide");

                    ccp.getBonusClassYearData(1, 1);
                }

                else {

                    /* $('#divBonusGrid').addClass("hide");*/
                    $('#divBonusGrid').closest('.sub-tabs').addClass("hide");
                    $('#divCompensationGrid').addClass("hide");
                    $('#divHistoryGrid').removeClass("hide");

                    var firstRowFirstColumnHeight = $('#tblCounselHistory thead:eq(0) tr:eq(0) th:eq(4)').outerHeight();
                    $('#tblCounselHistory thead tr:eq(1) th').css("top", firstRowFirstColumnHeight);

                    var secondRowFirstColumnHeight = firstRowFirstColumnHeight + $('#tblCounselHistory thead:eq(0) tr:eq(0) th:eq(4)').outerHeight();
                    $('#tblCounselHistory thead:eq(1) tr:eq(0) th').css("top", secondRowFirstColumnHeight);

                    common.tableAlternateColor(true, 'ttblCounselHistory');
                }
            }
            else {
                $('.tabParent .tab').removeClass("tab-selected");
                $(this).addClass("tab-selected");

                commonCompensation.getFreezeDetails();
            }
        });

        $('#divBonusGrid').on('change', '#ddlVersionFilter', function (e) {
            $('.divInnerPopup').css('display', 'flex');
            $('#divBonusGrid').html("");

            ccp.getBonusClassYearData($(this).val(), 0);
        });

        $('#divHistoryGrid').on('click', '.exp-type', function () {
            var expType = $(this).attr("data-row");
            if ($(this).find('.exp-type-icon').hasClass('fa-minus')) {
                $('#tblCounselHistory .' + expType).css('display', 'none');
                $(this).find('.exp-type-icon').removeClass('fa-minus').addClass('fa-plus');
            }
            else {
                $('#tblCounselHistory .' + expType).css('display', 'table-row');
                $(this).find('.exp-type-icon').addClass('fa-minus').removeClass('fa-plus');
                $('#tblCounselHistory .' + expType).find('.exp-type-icon').addClass('fa-minus').removeClass('fa-plus');
            }
        });

        $('#tblCounselHistory th.sort').click(function () {

            var cls = '';
            if ($(this).hasClass('sorting')) {
                cls = 'sorting_asc';
            }
            else if ($(this).hasClass('sorting_asc')) {
                cls = 'sorting_desc';
            }
            else if ($(this).hasClass('sorting_desc')) {
                cls = 'sorting_asc';
            }

            $('#tblCounselHistory th.sort').removeClass('sorting_asc').removeClass('sorting_desc').addClass('sorting');

            $(this).removeClass('sorting').addClass(cls);

            var element = $('#tblCounselHistory tbody');
            var col = $(this).attr('data-attr-ind');
            var sortOrder = $(this).hasClass('sorting_asc') ? 'asc' : 'desc';

            var count = $('#tblCounselHistory tbody').find("[data-row='partner1']").index() + 1;
            ccp.sortElementData(element, count, col, sortOrder, '.partner1');

            count = $('#tblCounselHistory tbody').find("[data-row='partner2']").index() + 1;
            ccp.sortElementData(element, count, col, sortOrder, '.partner2');

            //ccp.ManageStickyHeader();
        });

        $('.sub-tabs').on('click', '.month-tab', function (e) {
            e.stopImmediatePropagation();
            $('#divBonusGrid').parent().find('.month-tab').removeClass('mth-selected');
            $(this).addClass(' mth-selected');
            ccp.getBonusClassYearData($('#ddlVersionFilter').val(), 1);
        });
        //$('#tblCounselCompensationPlanning').on('change', 'input[type=text]', function (e) {
        //    flag = true;
        //    $(this).closest('tr').find('.hdnIsUpdated').val('true');
        //});

        $('#tblCounselCompensationPlanning .performanceScoreShowIcon').tooltip({
            items: '*',
            content: function () {
                var $tr = $(this).closest('tr');
                var html = "<div class='divPerformanceScore' style='display:inline-block;z-index:9999'>" + $tr.find('.divPerformanceScore').html() + "</div>";
                return html;
            },
            position: {
                my: "right bottom",
                at: "right top"
            },
            show: {
                effect: "fade",
                duration: 200
            },
            hide: {
                effect: "fade",
                duration: 200
            },
            open: function (event, ui) {
                var $element = $(event.target);
                ui.tooltip.click(function () {
                    $element.tooltip('close');
                });
                $(document).mouseup(function (e) {
                    if ($element.data('ui-tooltip')) {
                        var container = $(".ui-tooltip");
                        if (!container.is(e.target) && container.has(e.target).length === 0) {
                            $element.tooltip('close');
                        }
                    }
                });
                $(document).mouseover(function (e) {
                    if ($element.data('ui-tooltip')) {
                        var container = $(".ui-tooltip");
                        if (!container.is(e.target) && container.has(e.target).length === 0 && !$('.performanceScoreShowIcon').is(e.target)) {
                            $element.tooltip('close');
                        }
                    }
                });
            },

        })
            .on('mouseout focusout', function (event) {
                event.stopImmediatePropagation();
            });

        $('#tblCounselCompensationPlanning .billableHrsShowIcon').tooltip({
            items: '*',
            content: function () {
                var $tr = $(this).closest('tr');
                var html = "<div class='divPerformanceScore' style='display:inline-block;z-index:9999'>" + $tr.find('.divBillableHrs').html() + "</div>";
                return html;
            },
            position: {
                my: "right bottom",
                at: "right top"
            },
            show: {
                effect: "fade",
                duration: 200
            },
            hide: {
                effect: "fade",
                duration: 200
            },
            open: function (event, ui) {
                var $element = $(event.target);
                ui.tooltip.click(function () {
                    $element.tooltip('close');
                });
                $(document).mouseup(function (e) {
                    if ($element.data('ui-tooltip')) {
                        var container = $(".ui-tooltip");
                        if (!container.is(e.target) && container.has(e.target).length === 0) {
                            $element.tooltip('close');
                        }
                    }
                });
                $(document).mouseover(function (e) {
                    if ($element.data('ui-tooltip')) {
                        var container = $(".ui-tooltip");
                        if (!container.is(e.target) && container.has(e.target).length === 0 && !$('.billableHrsShowIcon').is(e.target)) {
                            $element.tooltip('close');
                        }
                    }
                });
            },

        })
            .on('mouseout focusout', function (event) {
                event.stopImmediatePropagation();
            });

        $('#tblCounselCompensationPlanning .bonusCollectionsShowIcon').tooltip({
            items: '*',
            content: function () {
                var $tr = $(this).closest('tr');
                var html = "<div class='divBonusCollections' style='display:inline-block;z-index:9999'>" + $tr.find('.divBonusCollections').html() + "</div>";
                return html;
            },
            position: {
                my: "right bottom",
                at: "right top"
            },
            show: {
                effect: "fade",
                duration: 200
            },
            hide: {
                effect: "fade",
                duration: 200
            },
            open: function (event, ui) {
                var $element = $(event.target);
                ui.tooltip.click(function () {
                    $element.tooltip('close');
                });
                $(document).mouseup(function (e) {
                    if ($element.data('ui-tooltip')) {
                        var container = $(".ui-tooltip");
                        if (!container.is(e.target) && container.has(e.target).length === 0) {
                            $element.tooltip('close');
                        }
                    }
                });
                $(document).mouseover(function (e) {
                    if ($element.data('ui-tooltip')) {
                        var container = $(".ui-tooltip");
                        if (!container.is(e.target) && container.has(e.target).length === 0 && !$('.bonusCollectionsShowIcon').is(e.target)) {
                            $element.tooltip('close');
                        }
                    }
                });
            },

        })
            .on('mouseout focusout', function (event) {
                event.stopImmediatePropagation();
            });



        $(document).ready(function () {
            //show selected filter
            if ($('.sel-in-top').length > 0) {
                $('.dv-fil-top-sel').css('display', 'block');
                $('.cl-filter').css('display', 'inline-block');
            }

            $.fn.dataTable.ext.order['dom-text-numeric'] = function (settings, col) {
                return this.api()
                    .column(col, { order: 'index' })
                    .nodes()
                    .map(function (td, i) {
                        return $('input', td).val().replace(/,/g, '') * 1;
                    });
            };
            var colCount = [];
            $('#tblCounselCompensationPlanning thead:eq(1) tr .sort-input').each(function () {
                colCount.push($(this).index());
            });

            table = $('#tblCounselCompensationPlanning').DataTable({
                paging: false,
                ordering: true,
                bAutoWidth: false,
                searching: true,
                "order": [[1, "asc"], [6, "asc"], [0, "asc"]],
                columnDefs: [
                    { targets: 4, type: 'date' },
                    { targets: colCount, orderDataType: 'dom-text-numeric' }
                ],
            });

            if ($('.tblCounselCompensationPlanning-wrapper').length == 0) {
                $('#tblCounselCompensationPlanning').wrap("<div class='tblCounselCompensationPlanning-wrapper'></div>");
                $('<div class="dt-buttons"><button class="dt-button buttons-excel buttons-html5" tabindex="0" data-controls="tblCounselCompensationPlanning" type="button" onclick="ccp.exportToExcel()"><span>Download XLS</span></button></div>').insertBefore($('#tblCounselCompensationPlanning_filter'));

                //Button Freeze Only For Super User and Current Year and last Year
                if (parseInt($('.year-tab.tab-selected').attr('data-year').trim()) == (new Date().getFullYear()) && parseInt($('#hdnActualRoleID').val()) == userRole.SuperAdmin) {
                    //$('<button id="btnFreezeCompensation" style="margin-top:0px" class="dt-button buttons-dark">Freeze</button>').insertBefore($('#tblCounselCompensationPlanning_filter'));
                    $('<div style="display: inline-block; vertical-align: top;">' +
                        '<button id="btnFreezeCompensation" style="margin-top:0px;margin-right: 10px;"  class="dt-button buttons-dark">Freeze</button>' +
                        '<button class="btn btn-secondary btn-md kt-font-bold kt-font-transform-u" id="addNewEmployee" style="margin-top:0px;">Add New</button>' +
                        '</div>').insertBefore($('#tblCounselCompensationPlanning_filter'));
                }
                else if (parseInt($('.year-tab.tab-selected').attr('data-year').trim()) == (new Date().getFullYear())) {
                    //$('<button id="btnFreezeCompensation" style="margin-top:0px" class="dt-button buttons-dark">Freeze</button>').insertBefore($('#tblCounselCompensationPlanning_filter'));
                    $('<div style="display: inline-block; vertical-align: top;">' +
                        '<button class="btn btn-secondary btn-md kt-font-bold kt-font-transform-u" id="addNewEmployee" style="margin-top:0px;">Add New</button>' +
                        '</div>').insertBefore($('#tblCounselCompensationPlanning_filter'));
                }

            }

            $('.tblCounselCompensationPlanning-wrapper').css('max-height', ($(window).height() * 0.85) + 'px');

            $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(0) th:eq(0)').css({
                'left': '0px',
                'position': 'sticky !important',
                'z-index': '3'
            });

            if ($.fn.DataTable.isDataTable("#tblCounselHistory")) {
                $('#tblCounselHistory').DataTable().clear().destroy();
            }

            $('#tblCounselHistory').DataTable({
                paging: false,
                ordering: false,
                bAutoWidth: false,
                searching: false,
            });
            if ($('.tblCounselHistory-wrapper').length == 0)
                $('#tblCounselHistory').wrap("<div class='tblCounselHistory-wrapper'></div>");

            $('.tblCounselHistory-wrapper').css('max-height', ($(window).height() * 0.65) + 'px');

            ccp.manageHeaderTotal();

            $('#tblCounselCompensationPlanning .tdBillableCreditcallasped').css('display', 'none')
            $('#tblCounselCompensationPlanning .thBillableCredit').css('display', 'none');
            var currentYearHeaderMainColspan = parseInt($('.currentYearHeaderMain').attr("colspan")) - 8;
            $('.currentYearHeaderMain').attr("colspan", currentYearHeaderMainColspan);

            ccp.manageCounselCompensationPlanningFixedHeader();

            if ($('.tabParent .freezedTab').hasClass('tab-selected')) {
                $('.tabParent .freezedTab.tab-selected').click();
            }

            $('.btn-carry-filter').css('display', 'none');
        })

        setTimeout(function () {
            ccp.loadFromLocalStorage();
        }, 500);
    },

    IsExtraColumnValidate: function () {
        try {
            var IsValidate = true;
            if ($('.txtcolumnname').val() == '') {
                $('.dvcolumnname').css('display', 'block');
                IsValidate = false;

            }
            if ($('.datatype').val() == '0') {
                $('.dvdatatype').css('display', 'block');
                IsValidate = false;
            }
            return IsValidate;
        } catch (err) {
            console.log(err);
            return false;
        }
    },

    CheckDuplicateExtraColumnName: function () {
        try {
            var Year = $('.tabParent .tab-selected').attr('data-year');
            var formData = new FormData();
            formData.append("ColumnName", $('.txtcolumnname').val());
            formData.append("ID", parseInt($('.hdnID').val()));
            formData.append("FormID", 2);
            formData.append("Year", Year);

            axios.post(gWebsiteURL + "report/CheckDuplicateExtraColumnName", formData, getAxiosConfigHeader())
                .then(function (response) {
                    if (response.status == 200 && response.data.length > 0) {
                        var Result = JSON.parse(response.data)
                        if (Result[0].DuplicateCount > 0) {
                            $('.dvcolumnname').text('Name already exists.');
                            $('.dvcolumnname').css('display', 'block');
                        }
                        else if (Result[0].YearEndCount >= 5 && parseInt($('.hdnID').val()) <= 0 && $('.hdnIsYearEnd').val() == "true") {
                            $('.dvcolumnname').text('can not add column more than 5.');
                            $('.dvcolumnname').css('display', 'block');
                        }
                        else if (Result[0].OtherCount >= 5 && parseInt($('.hdnID').val()) <= 0 && $('.hdnIsYearEnd').val() == "false") {
                            $('.dvcolumnname').text('can not add column more than 5.');
                            $('.dvcolumnname').css('display', 'block');
                        }
                        else {
                            $('.dvcolumnname').text('');
                            $('.dvcolumnname').css('display', 'none');
                            //ccp.saveExtraColumn();
                            if (flag)
                                $("#dvUnsavedChangesConfirmation").modal("show");
                            else
                                ccp.saveExtraColumn();

                        }
                    }
                })
                .catch(function (error) {
                    console.log(error);
                });
        } catch (err) {
            console.log(err);
        }
    },

    setSubTotalBonusNFinalCompensation: function (obj, dataType, Type, allowMinus) {
        try {
            if (dataType.toLowerCase() == 'int' || dataType.toLowerCase() == 'decimal') {
                var values = $(obj).val() == "" ? "" : (allowMinus == 'allowMinus' ? $(obj).val().replace(/,/g, '') : $(obj).val().replace(/,/g, '').replace('-', ''));
                //if ($(obj).val() != '' && allowMinus != 'allowMinus') {
                //    var rexvalues = values;
                //    var reg = dataType.toLowerCase() == 'int' ? new RegExp(/^\d{0,9}$/) : new RegExp(/^\d{0,9}(\.\d{1,2})?$/);

                //    if (!reg.test(rexvalues)) {
                //        $(obj).val('');
                //        values = 0;
                //    }
                //}
                if (values != "") {
                    values = common.numberWithCommas(values);
                    //values = common.numberWithCommas(parseInt(values));
                    $(obj).val(values);
                }
                var $tr = $(obj).closest('tr');
                if (Type == 'AdjYEBonus') {
                    commonCompensation.getAdjYEBonus($tr);
                }

                var subTotal = ccp.getSubTotalBonus($tr, true);
                $tr.find('.AxinnsubTotal').text(common.numberWithCommasDash(subTotal));
                var otherBonusTotal = ccp.getSubTotalBonus($tr, false);
                $tr.find('.bonusOtherSubTotal').html(common.numberWithCommasDash(otherBonusTotal));

                var annualSalary = $tr.find('.AnnualSalary').text().replace(/,/g, '').replace('-', '');
                annualSalary = annualSalary == '' ? 0 : parseFloat(annualSalary);

                var timeEntryPenalty = $tr.find('.yearpenalty').text().replace(/,/g, '').replace('-', '');
                timeEntryPenalty = timeEntryPenalty == '' ? 0 : parseFloat(timeEntryPenalty);

                var totalCompensation = subTotal == "" ? (annualSalary + otherBonusTotal) : (annualSalary + subTotal - timeEntryPenalty + otherBonusTotal);

                var totalDeductedBonus = subTotal == "" ? 0 : (subTotal - timeEntryPenalty);
                $tr.find('.deductedbonus').text(common.numberWithCommasDash(totalDeductedBonus));
                $tr.find('.bonusTotal').text(common.numberWithCommasDash(totalDeductedBonus + otherBonusTotal));

                $tr.find('.fcompensation').text(common.numberWithCommasDash(totalCompensation));

                //let table = $('.tblCompensationCommon').DataTable();
                //table.row($tr).invalidate().draw();
            }
        } catch (err) {
            console.log(err);
        }
    },

    getTotalSpecialBonus: function (objtr, className) {
        try {
            var totalSpecialBonus = 0;
            $(objtr).find('.' + className + '').each(function () {
                totalSpecialBonus += isNaN(parseFloat($(this).val().replace(/,/g, ''))) ? 0 : parseFloat($(this).val().replace(/,/g, ''));
            });

            return totalSpecialBonus;

        } catch (err) {
            console.log(err);
            return 0;
        }
    },

    getSubTotalBonus: function (objtr, isYearEnd) {
        try {
            var TotalSpecialBonus = ccp.getTotalSpecialBonus(objtr, 'extracolumn');
            var totalSpecialBonusOther = ccp.getTotalSpecialBonus(objtr, 'extracolumn-other');
            if (isYearEnd) {
                TotalSpecialBonus = TotalSpecialBonus - totalSpecialBonusOther;
            }

            var SubTotal = 0.0;
            if (isYearEnd) {
                var AdjYEBonus = objtr.find('.AdjYEBonus').text().replace(/,/g, '');
                AdjYEBonus = AdjYEBonus == '' ? 0 : isNaN(parseFloat(AdjYEBonus)) ? 0 : parseFloat(AdjYEBonus);

                var OtherBonus = objtr.find('.txtSpecialBonus').val().replace(/,/g, '').replace('-', '');
                OtherBonus = OtherBonus == '' ? 0 : parseFloat(OtherBonus);

                SubTotal = (AdjYEBonus + OtherBonus + TotalSpecialBonus);
            }
            else {
                SubTotal = totalSpecialBonusOther + (objtr.find('.hourBonus').length > 0 ? common.getValueControlDiv(objtr.find('.hourBonus'), false) : 0);
            }

            return SubTotal;

        } catch (err) {
            console.log(err);
            return 0;
        }
    },

    setAdjCompensation: function (obj) {
        var $tr = $(obj).closest('tr');
        var adjNextYear = $tr.find('.txtNextYearAdj').val().replace(/,/g, '');
        adjNextYear = (adjNextYear == '' ? 0 : adjNextYear);
        adjNextYear = (isNaN(parseFloat(adjNextYear)) ? 0 : parseFloat(adjNextYear));

        var CompensationNextYear = $tr.find('.CompensationNextYear:last').text().replace(/,/g, '').replace('-', '');
        CompensationNextYear = CompensationNextYear == '' ? 0 : parseFloat(CompensationNextYear);

        var adjCompensation = (CompensationNextYear + adjNextYear);
        $tr.find('.adjCompensation').text(common.numberWithCommasDash(adjCompensation.toFixed(0)));
    },

    setFormatValue: function (obj) {
        var values = $(obj).val() == "" ? 0.00 : $(obj).val().replace(/,/g, '');
        if ($(obj).val() != '') {
            var rexvalues = values;
            var reg = new RegExp(/^\d{1,3}(\.\d{1,2})?$/);
            var reg1 = new RegExp(/^\d{0}(\.\d{1,2})?$/);
            if (reg1.test(rexvalues)) {
                rexvalues = '0' + rexvalues;
                $(obj).val(rexvalues);
            }

            if (!reg.test(rexvalues)) {
                $(obj).val('');
                return false;
            }
            else {
                values = rexvalues
            }
        }
        $(obj).val(common.numberWithCommas(parseFloat(values).toFixed(2)));
    },

    setFormatValueWithNegtive: function (obj) {
        var values = $(obj).val() == "" ? 0.00 : $(obj).val().replace(/,/g, '');
        if ($(obj).val() != '') {
            var rexvalues = values;
            var reg = new RegExp(/^-?\d{0,9}(\.\d{1,2})?$/);
            var reg1 = new RegExp(/^-?\d{0}(\.\d{1,2})?$/);
            if (reg1.test(rexvalues)) {
                rexvalues = '0' + rexvalues;
                $(obj).val(rexvalues);
            }

            if (!reg.test(rexvalues)) {
                $(obj).val('');
                return false;
            }
            else {
                values = rexvalues
            }
        }
        $(obj).val(common.numberWithCommas(parseFloat(values).toFixed(2)));
    },

    saveCounselCompensationPlanning: function () {
        try {
            var lstCC = [];
            var rowCount = 0;
            if (!commonCompensation.inputControlValidation()) {
                return false;
            }

            //$('#tblCounselCompensationPlanning').find('.dvErrMsgFTE').each(function () {
            //    if ($(this).html() != "") {
            //        rowCount = $(this).closest("tr").index() + 1;
            //        Swal.fire("", "FTE cannot be more than 1.00 in row " + rowCount, "error");
            //        return false;
            //    }
            //})
            $('#tblCounselCompensationPlanning').find('.dvErrMsgFTE').each(function () {
                if ($(this).html() != "") {
                    rowCount = $(this).closest("tr").index() + 1;
                    var $tr = $(this).closest('tr');
                    var EmployeeName = $tr.find('.EmployeeName').text().trim();
                    var nextFTE = $tr.find('.txtNextYearFTE').val();
                    if (nextFTE < -1.00) {
                        Swal.fire("", "FTE cannot be less than -1.00 for " + EmployeeName, "error");
                    } else {
                        Swal.fire("", "FTE cannot be more than 1.00 for " + EmployeeName, "error");
                    }
                    return false;
                }
            })
            let isContinue = true
            // Push Data In List     
            if (rowCount == 0) {
                $('#tblCounselCompensationPlanning tbody tr.loopRow').each(function () {
                    var ID = $(this).attr('data-id');
                    if ($(this).find('.hdnIsUpdated').val() == 'true') {
                        //var Name = $(this).find('.name').text();
                        var Name = $(this).find('.EmployeeName').text().trim() == "" ? $(this).find('.EmployeeName').val() : $(this).find('.EmployeeName').text().trim();

                        if (!commonCompensation.checkForDuplicates(Name, $tr)) {
                            isContinue = false;
                        }

                        var PracticeGroup = $(this).find('.selPracticeGroup').length > 0 ? $(this).find('.selPracticeGroup').val() : $(this).find('.td-practice-group').text().trim();
                        var Office = $(this).find('.selOffice').length > 0 ? $(this).find('.selOffice').val() : $(this).find('.td-office').text().trim();
                        var JdYear = $(this).find('.txtJdYear').length > 0 ? $(this).find('.txtJdYear').val() : $(this).find('.td-txtJdYear').text().trim();
                        var HireDate = $(this).find('.hireDate').length > 0 ? $(this).find('.hireDate').val() : $(this).find('.t-HireDate').text().trim();
                        var AnnualCompensation = $(this).find('.inpNextYearAnnualComp').length > 0 ? $(this).find('.inpNextYearAnnualComp').val().replace(/,/g, '') : $(this).find('.txtcompensation').text().trim();
                        var DiscretionaryBonusCollections = $(this).find('.ddlDiscretionaryBonusCollections').val() || '0';
                        var DiscretionaryBonusBillables = $(this).find('.ddlDiscretionaryBonusBillables').val() || '0';
                        var AssocReferralProgram = $(this).find('.ddlAssocReferralProgram').val() || '0';
                        var NextAxinnRole = $(this).find('.ddlNextAxinnRole').val();
                        var compensation = $(this).find('.txtcompensation').val().replace(/,/g, "");
                        var PGChairComments = $(this).find('.txtComments').val();
                        var NextYearFTE = $(this).find('.txtNextYearFTE').val();
                        if (NextYearFTE != '' && parseFloat(NextYearFTE) > 1) {
                            fteFlag = true;
                        }
                        var Adjustment = $(this).find('.txtAdjustment').val().replace(/,/g, "");
                        var AdjComments = $(this).find('.txtAdjComments').val();

                        var currentYearMidYearScore = $(this).find('.txtCurrentYearMidYearScore').val().replace(/,/g, "").replace(/-/g, "");
                        var currentYearYEScore = $(this).find('.txtCurrentYearYEScore').val().replace(/,/g, "").replace(/-/g, "");

                        var SpecialBonus = 0;
                        if ($(this).closest('tr').find('.txtSpecialBonus').val().replace(/,/g, "").replace(/-/g, "") != "")
                            SpecialBonus = $(this).closest('tr').find('.txtSpecialBonus').val().replace(/,/g, "").replace(/-/g, "");

                        var Year = $('.year-tab.tab-selected').attr('data-year').trim();

                        var $tr = $(this).closest('tr');
                        var Column1 = $tr.find('.txtColumn1');
                        var Column2 = $tr.find('.txtColumn2');
                        var Column3 = $tr.find('.txtColumn3');
                        var Column4 = $tr.find('.txtColumn4');
                        var Column5 = $tr.find('.txtColumn5');
                        var Column6 = $tr.find('.txtColumn6');
                        var Column7 = $tr.find('.txtColumn7');
                        var Column8 = $tr.find('.txtColumn8');
                        var Column9 = $tr.find('.txtColumn9');
                        var Column10 = $tr.find('.txtColumn10');

                        Column1 = Column1.length > 0 ? Column1.val().replace(/,/g, '') == '' ? '0' : Column1.val().replace(/,/g, '') : 0;
                        Column2 = Column2.length > 0 ? Column2.val().replace(/,/g, '') == '' ? '0' : Column2.val().replace(/,/g, '') : 0;
                        Column3 = Column3.length > 0 ? Column3.val().replace(/,/g, '') == '' ? '0' : Column3.val().replace(/,/g, '') : 0;
                        Column4 = Column4.length > 0 ? Column4.val().replace(/,/g, '') == '' ? '0' : Column4.val().replace(/,/g, '') : 0;
                        Column5 = Column5.length > 0 ? Column5.val().replace(/,/g, '') == '' ? '0' : Column5.val().replace(/,/g, '') : 0;
                        Column6 = Column6.length > 0 ? Column6.val().replace(/,/g, '') == '' ? '0' : Column6.val().replace(/,/g, '') : 0;
                        Column7 = Column7.length > 0 ? Column7.val().replace(/,/g, '') == '' ? '0' : Column7.val().replace(/,/g, '') : 0;
                        Column8 = Column8.length > 0 ? Column8.val().replace(/,/g, '') == '' ? '0' : Column8.val().replace(/,/g, '') : 0;
                        Column9 = Column9.length > 0 ? Column9.val().replace(/,/g, '') == '' ? '0' : Column9.val().replace(/,/g, '') : 0;
                        Column10 = Column10.length > 0 ? Column10.val().replace(/,/g, '') == '' ? '0' : Column10.val().replace(/,/g, '') : 0;
                        var isNewEmployee = $(this).find('.hdnIsNewEmployee').length > 0 ? $tr.find('.hdnIsNewEmployee').val() : false;

                        lstCC.push({
                            'ID': parseInt(ID),
                            'Name': Name,
                            'EmployeeCode': $tr.find('.EmployeeCode').length > 0 ? $tr.find('.EmployeeCode').html() : Name,
                            'PracticeGroup': PracticeGroup,
                            'Office': Office,
                            'JdYear': JdYear,
                            'HireDate': HireDate,
                            'AnnualCompensation': AnnualCompensation,
                            'DiscretionaryBonusCollections': DiscretionaryBonusCollections,
                            'DiscretionaryBonusBillables': DiscretionaryBonusBillables,
                            'AssocReferralProgram': AssocReferralProgram,
                            'NextAxinnRole': NextAxinnRole,
                            'NextAxinnBase': 0,
                            'PGChairComments': PGChairComments,
                            'FTE': isNaN(parseFloat(NextYearFTE == '' ? 0 : NextYearFTE)) ? 0 : parseFloat(NextYearFTE == '' ? 0 : NextYearFTE),
                            'Adjustment': isNaN(parseFloat(Adjustment == '' ? 0 : Adjustment)) ? 0 : parseFloat(Adjustment == '' ? 0 : Adjustment),
                            'AdjustmentComments': AdjComments,
                            'NextYearAdj': 0,
                            'nextYearAdjComment': '',
                            'SpecialBonus': parseFloat(SpecialBonus == '' ? 0 : SpecialBonus),
                            'compensation': isNaN(parseFloat(compensation == '' ? 0 : compensation)) ? 0 : parseFloat(compensation == '' ? 0 : compensation),
                            'CurrentYearMidYearScore': isNaN(parseFloat(currentYearMidYearScore == '' ? 0 : currentYearMidYearScore)) ? 0 : parseFloat(currentYearMidYearScore == '' ? 0 : currentYearMidYearScore),
                            'CurrentYearYEScore': isNaN(parseFloat(currentYearYEScore == '' ? 0 : currentYearYEScore)) ? 0 : parseFloat(currentYearYEScore == '' ? 0 : currentYearYEScore),

                            'Column1': isNaN(parseFloat(Column1)) ? 0 : Column1,
                            'Column2': isNaN(parseFloat(Column2)) ? 0 : Column2,
                            'Column3': isNaN(parseFloat(Column3)) ? 0 : Column3,
                            'Column4': isNaN(parseFloat(Column4)) ? 0 : Column4,
                            'Column5': isNaN(parseFloat(Column5)) ? 0 : Column5,
                            'Column6': isNaN(parseFloat(Column6)) ? 0 : Column6,
                            'Column7': isNaN(parseFloat(Column7)) ? 0 : Column7,
                            'Column8': isNaN(parseFloat(Column8)) ? 0 : Column8,
                            'Column9': isNaN(parseFloat(Column9)) ? 0 : Column9,
                            'Column10': isNaN(parseFloat(Column10)) ? 0 : Column10,
                            'Year': Year,
                            'AdjustmentHrs': common.getValueControl($(this).find('.txtAdjustmentHrs'), $(this).find('.txtAdjustmentHrs').closest('td').find('.toggle-btn.toggle-amt').hasClass('active') ? true : false),
                            'AdjustmentHrsComments': $(this).find('.AdjustmentHrsComments').val(),
                            'IsAdjustedHrsPercentage': $(this).find('.txtAdjustmentHrs').closest('td').find('.toggle-btn.toggle-amt').hasClass('active') ? false : true,
                            'IsAdjusted_YearEnd_Percentage': $(this).find('.txtAdjustment').closest('td').find('.toggle-btn.toggle-amt').hasClass('active') ? false : true,
                            'IsNewEmployee': isNewEmployee
                        })
                    }
                });
                if (!isContinue) {
                    return false;
                }
                // save in DB
                if (fteFlag == false) {
                    if (lstCC.length > 0) {
                        var formData = new FormData();
                        formData.append("lstCC", JSON.stringify(lstCC));

                        axios.post(gWebsiteURL + "report/insert-update-Counsel-Compensation", formData, getAxiosConfigHeader())
                            .then(function (response) {
                                if (response.status == 200 && response.data != -1) {
                                    ccp.clearLocalStorage();
                                    $('#tblCounselCompensationPlanning tbody tr').each(function () {
                                        $(this).find('.hdnIsUpdated').val('false');
                                    });
                                    if (flag) {
                                        if (parseInt($('#hdnDeleteColumnUnsavedChangesYes').val()) == 1) {
                                            ccp.inactiveExtraColumn();
                                        }
                                        else {
                                            if ($('.txtcolumnname').val() != '') {
                                                ccp.saveExtraColumn();
                                            } else {
                                                flag = false;
                                                fteFlag = false
                                                Swal.fire("", " Record Successfully Saved!", "success");
                                                window.location.href = window.location.href;
                                            }
                                        }
                                    } else {
                                        flag = false;
                                        fteFlag = false
                                        Swal.fire("", " Record Successfully Saved!", "success");
                                        window.location.href = window.location.href;
                                    }

                                }
                                else {
                                    Swal.fire("", "Internal server error...", "error");
                                }
                            })
                            .catch(function (error) {
                                console.log(error);
                            });
                    }
                    else {
                        Swal.fire("", "Nothing to save!", "warning");
                    }
                }
            }
        }
        catch (err) {
            console.log(err);
        }
    },

    saveExtraColumn: function () {
        try {
            var Year = $('.year-tab.tab-selected').attr('data-year').trim();

            var formData = new FormData();
            formData.append("ColumnName", $('.txtcolumnname').val());
            formData.append("DataType", $('.datatype').val());
            formData.append("ID", parseInt($('.hdnID').val()));
            formData.append("FormID", 2);
            formData.append("Year", Year);
            formData.append("IsYearEnd", $('.hdnIsYearEnd').val());

            axios.post(gWebsiteURL + "report/InsertExtraColumn", formData, getAxiosConfigHeader())
                .then(function (response) {
                    if (response.status == 200 && response.data != -1) {
                        $("#dvAddNewColumn").modal("hide");
                        $("#dvUnsavedChangesConfirmation").modal("hide");
                        Swal.fire("", "Record Successfully Saved!", "success");
                        window.location.href = window.location.href;
                    }
                    else {
                        Swal.fire("", "Internal server error...", "error");
                    }
                })
                .catch(function (error) {
                    console.log(error);
                });
        } catch (err) {
            console.log(err);
        }
    },

    inactiveExtraColumn: function () {
        try {
            var Year = $('.tabParent .tab-selected').attr('data-year');
            var formData = new FormData();
            formData.append("ID", parseInt($('.hdnID').val()));
            formData.append("FormID", 2);
            formData.append("Year", Year);

            axios.post(gWebsiteURL + "report/InactiveExtraColumn", formData, getAxiosConfigHeader())
                .then(function (response) {
                    if (response.status == 200 && response.data != -1) {
                        $("#dvAddNewColumn").modal("hide");
                        $("#dvUnsavedChangesConfirmation").modal("hide");
                        $("#dvConfirmation").modal("hide");
                        Swal.fire("", "Removed Extra Column Successfully!", "success");
                        window.location.href = window.location.href;
                    }
                    else {
                        Swal.fire("", "Internal server error...", "error");
                    }
                })
                .catch(function (error) {
                    console.log(error);
                });
        } catch (err) {
            console.log(err);
        }
    },

    exportToExcel: function () {
        $('#divExcelImport').html('<table>' + $('#tblCounselCompensationPlanning').html() + '</>');

        $('#divExcelImport table thead th.thBillableCredit').each(function () {
            if ($(this).css('display') == 'none')
                $(this).css('display', 'table-cell');
        })

        $('#divExcelImport table thead th.afterBillableCollapsed').each(function () {
            if ($(this).css('display') == 'none')
                $(this).css('display', 'table-cell');
        })

        $('#divExcelImport table tbody td.tdBillableCreditcallasped').each(function () {
            if ($(this).css('display') == 'none')
                $(this).css('display', 'table-cell');
        })

        $('#divExcelImport table thead th.thBillableCreditExpend').each(function () {
            $(this).remove();
        })

        $('#divExcelImport table tbody td.tdBillableCreditcallExpend').each(function () {
            $(this).remove();
        })

        $('#divExcelImport .ddlDiscretionaryBonusCollections').each(function () {
            $(this).parents('td').html($(this).find('option:selected').text()).css("text-align", "left");
        })

        $('#divExcelImport .ddlDiscretionaryBonusBillables').each(function () {
            $(this).parents('td').html($(this).find('option:selected').text()).css("text-align", "left");
        })

        $('#divExcelImport .ddlAssocReferralProgram').each(function () {
            $(this).parents('td').html($(this).find('option:selected').text()).css("text-align", "left");
        })

        $('#divExcelImport .ddlNextAxinnRole').each(function () {
            try {
                $(this).parents('td').html($(this).find('option:selected').text()).css("text-align", "left");
            }
            catch (err) {
                console.log(err);
            }
        })

        $('#divExcelImport .txtNextYearFTE').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtcompensation').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtAdjustment ').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtAdjustmentHrs ').each(function () {
            var parentsTd = $(this).parents('td');
            var html = $(this).val();
            if (html != "-") {
                if ($(parentsTd).find('.toggle-btn.active').hasClass("toggle-per")) {
                    html = html + "%";
                }
            }
            $(parentsTd).html(html);
        })

        $('#divExcelImport .txtAdjComments').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .AdjustmentHrsComments').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtcompensation').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtSpecialBonus').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtComments').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtCurrentYearMidYearScore ').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtCurrentYearYEScore').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtCurrentYearMidYearScore').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtSpecialBonus').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn1').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn2').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn3').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn4').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn5').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport .txtColumn6').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn7').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn8').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn9').each(function () {
            $(this).parents('td').html($(this).val());
        })
        $('#divExcelImport .txtColumn10').each(function () {
            $(this).parents('td').html($(this).val());
        })

        $('#divExcelImport table thead th').each(function () {
            if ($(this).html().trim() == "-") {
                $(this).html("0");
            }

            if ($(this).css('display') == 'none')
                $(this).remove();
        });

        $('#divExcelImport table tbody td').each(function () {
            if ($(this).hasClass('totalColumn') && $(this).html().trim() == "-") {
                $(this).html("0");
            }

            if ($(this).css('display') == 'none')
                $(this).remove();

            $(this).css(" vertical-align", "middle");
        })

        $('#divExcelImport table tbody td').each(function () {
            $(this).find('span.EmployeeCode').remove();
        });

        $('#divExcelImport .t-a-c').css("text-align", "center");
        $('#divExcelImport .t-a-r').css("text-align", "right");

        $('#divExcelImport table thead:eq(0) tr:eq(3)').removeAttr('aria-label');
        $('#divExcelImport table thead:eq(0) tr:eq(3)').find('.fa-info-circle').remove();
        $('#divExcelImport table thead:eq(0) tr:eq(3)').find('.sr-only').remove();

        $('#divExcelImport').find('.info-tooltip-leaves').remove();
        $('#divExcelImport').find('.info-tooltip-billableCredit').remove();

        $('#divExcelImport').find('.divBonusCollections').remove();
        $('#divExcelImport').find('.fa-info-circle').remove();
        $('#divExcelImport').find('.FC span.sr-only').remove();
        $('#divExcelImport').find('i').remove();


        var thHtml = "";
        $('#divExcelImport thead:eq(0) tr:eq(0) th').each(function (index) {
            var tr0ThObj = $(this);
            var tr0ThText = $(tr0ThObj).html();
            if ($(tr0ThObj).hasClass("headerWithColSpan")) {
                $('#divExcelImport thead:eq(0) tr:eq(1)').find('.' + $(tr0ThObj).attr("data-column")).each(function () {
                    var tr1ThObj = $(this);
                    var tr1ThObjText = $(tr1ThObj).html();
                    if ($(tr1ThObj).hasClass("headerWithColSpan")) {
                        if ($(tr1ThObj).attr("data-column") == "Salary") {
                            $('#divExcelImport thead:eq(0) tr:eq(3)').find('.' + $(tr1ThObj).attr("data-column")).each(function () {
                                var salaryThObj = $(this)
                                var salaryThObjText = $(salaryThObj).html();
                                if ($(salaryThObj).hasClass("headerWithColSpan")) {
                                    $('#divExcelImport thead:eq(0) tr:eq(4)').find('.' + $(salaryThObj).attr("data-column")).each(function () {
                                        var salaryIncreaseThObj = $(this)
                                        var salaryIncreaseThObjText = $(salaryIncreaseThObj).html();
                                        var thtext = tr0ThText + " " + tr1ThObjText + " " + salaryThObjText + " " + salaryIncreaseThObjText;
                                        thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                                    })
                                } else {
                                    var thtext = tr0ThText + " " + tr1ThObjText + " " + salaryThObjText;
                                    thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                                }
                            })
                        }
                        else if ($(tr1ThObj).attr("data-column") == "BonusDiscretionaryBonus") {
                            $('#divExcelImport thead:eq(0) tr:eq(4)').find('.' + $(tr1ThObj).attr("data-column")).each(function () {
                                var bonusDiscretionaryBonusThObj = $(this)
                                var bonusDiscretionaryBonusThObjText = $(bonusDiscretionaryBonusThObj).html();
                                var thtext = tr0ThText + " " + tr1ThObjText + " " + bonusDiscretionaryBonusThObjText;
                                thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                            })
                        }
                        else if ($(tr1ThObj).attr("data-column") == "BillableCredit") {
                            $('#divExcelImport thead:eq(0) tr:eq(3)').find('.' + $(tr1ThObj).attr("data-column")).each(function () {
                                var billableCreditThObj = $(this)
                                var billableCreditThObjText = $(billableCreditThObj).html();
                                if ($(billableCreditThObj).hasClass("headerWithColSpan")) {
                                    $('#divExcelImport thead:eq(0) tr:eq(4)').find('.' + $(billableCreditThObj).attr("data-column")).each(function () {
                                        var nonBillableCreditThObjTextThObj = $(this)
                                        var nonBillableCreditThObjText = $(nonBillableCreditThObjTextThObj).html();
                                        var thtext = tr0ThText + " " + tr1ThObjText + " " + billableCreditThObjText + " " + nonBillableCreditThObjText;
                                        thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                                    })
                                } else {
                                    var thtext = tr0ThText + " " + tr1ThObjText + " " + billableCreditThObjText;
                                    thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                                }
                            })
                        }

                        $('#divExcelImport thead:eq(0) tr:eq(2)').find('.' + $(tr1ThObj).attr("data-column")).each(function () {
                            var tr2ThObj = $(this);
                            var tr2ThObjText = $(tr2ThObj).html();
                            if ($(tr2ThObj).hasClass("headerWithColSpan")) {
                                $('#divExcelImport thead:eq(0) tr:eq(3)').find('.' + $(tr2ThObj).attr("data-column")).each(function () {
                                    var tr3ThObj = $(this);
                                    var tr3ThObjText = $(tr3ThObj).html();

                                    var thtext = tr0ThText + " " + tr1ThObjText + " " + tr2ThObjText + " " + tr3ThObjText;
                                    if ($(tr3ThObj).hasClass("BillableCreditNonBillableCreditableSubTotal"))
                                        thtext = tr0ThText + " " + tr1ThObjText + " " + tr2ThObjText + " " + $(tr3ThObj).find("div").html();
                                    thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                                })
                            }
                            else {
                                var thtext = tr0ThText + " " + tr1ThObjText + " " + tr2ThObjText;
                                if ($(tr2ThObj).hasClass("SpecialBonus"))
                                    thtext = tr0ThText + " " + tr1ThObjText + " " + $(tr2ThObj).find("div").html();
                                thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                            }
                        })
                    }
                    else {
                        var thtext = tr0ThText + " " + tr1ThObjText;
                        if ($(tr1ThObj).hasClass("thBillableCreditExpend"))
                            thtext = tr0ThText + " " + $(tr1ThObj).find(".thBillableCreditExpendText").html();
                        thHtml += "<th style='text-align:right'>" + common.removeRepeatedWords(thtext) + "</th>";
                    }
                })
            }
            else {
                thHtml += "<th style='text-align:left'>" + common.removeRepeatedWords(tr0ThText) + "</th>";
            }
        });

        $('#divExcelImport table thead:eq(0)').html(thHtml);

        htmls = $('#divExcelImport').html();
        $('#divExcelImport').html('');

        common.exportToExcelNew(htmls, null, "Counsel_Compensation_Planning.xlsx");
    },

    manageHeaderTotal: function () {
        try {
            var arrTotal = [];
            var maxCurrentYearDays = 0;
            $('#tblCounselCompensationPlanning tbody tr').each(function () {
                $(this).find('.totalColumn').each(function () {
                    var key = $(this).attr('data-attr-total-key');
                    var values = 0;
                    var inputObject = $(this).find('input[type=text]');
                    var divObject = $(this).find('.divSalaryCurrentYearIncrease');

                    if ($(inputObject).length > 0) {
                        values = $(inputObject).val() == "" || $(inputObject).val() == "-" ? 0 : key == "NextYearFTE" ? parseFloat($(inputObject).val().replace(/,/g, '')) : parseInt($(inputObject).val().replace(/,/g, ''));
                    }
                    else if ($(divObject).length > 0) {
                        values = $(divObject).html() == "" || $(divObject).html() == "-" ? 0 : parseInt($(divObject).html().replace(/,/g, ''));
                    }
                    else {
                        values = $(this).html() == "" || $(this).html() == "-" ? 0 : parseFloat($(this).html().replace(/,/g, ''));
                    }

                    if (key == "currentYearDays") {
                        if (maxCurrentYearDays < values) {
                            maxCurrentYearDays = values;
                        }
                    }
                    arrTotal.push({
                        [key]: values
                    });
                });
            })
            $('#tblCounselCompensationPlanning tbody tr:eq(0) .totalColumn').each(function () {
                var key = $(this).attr('data-attr-total-key');
                var totalValue = common.getTotalFromArray(arrTotal, key);
                if (key == "currentYearFTE" || key == "NextYearFTE" || key == "Adjustment") {
                    totalValue = totalValue.toFixed(2);
                }
                else if (key == "currentYearDays") {
                    totalValue = (maxCurrentYearDays).toFixed(2);
                }
                if (totalValue == 0)
                    $('#tblCounselCompensationPlanning thead:eq(1) tr .' + key).html('-');
                else
                    $('#tblCounselCompensationPlanning thead:eq(1) tr .' + key).html(common.numberWithCommas(totalValue));

            })

        } catch (err) {
            console.log(err);
        }
    },

    getBonusClassYearData: function (iVersion, iDefaultVersion) {
        var formData = new FormData();
        formData.append("iCompensationFormId", 2);
        formData.append("iVersion", iVersion);
        formData.append("iDefaultVersion", iDefaultVersion);
        formData.append("iBonusToggle", 0);
        formData.append("Year", $('#divBonusGrid').parent().find('.month-tab.mth-selected').attr('data-year'));

        axios.post(gWebsiteURL + "form/get-bonus-class-year-data", formData, getAxiosConfigHeader())
            .then(function (response) {
                if (response.status == 200 && response.data != -1) {
                    $('#divBonusGrid').html(response.data);

                    $('#tblAssociateBonus').DataTable({
                        paging: false,
                        ordering: false,
                        bAutoWidth: false,
                        searching: false
                    });

                    if ($('.dvAssociateBonus-wrapper').length == 0)
                        $('#tblAssociateBonus').wrap("<div class='dvAssociateBonus-wrapper'></div>");

                    $('.dvAssociateBonus-wrapper').css('max-height', ($(window).height() * 0.75) + 'px');


                    $('#tblAssociateBonus thead tr:eq(1) th').css('top', $('#tblAssociateBonus thead tr:eq(0) th:eq(1)').outerHeight());
                }

                setTimeout(function () {
                    $('.divInnerPopup').css('display', '');
                }, 2000)
            })
            .catch(function (error) {
                console.log(error);
                $('.divInnerPopup').css('display', '');
            });
    },

    getSalaryClassYearData: function (iVersion) {
        var formData = new FormData();
        formData.append("iCompensationFormId", 2);
        formData.append("iVersion", iVersion);
        formData.append("Year", $('#divSalaryGrid').parent().find('.month-tab.mth-selected').attr('data-year'));

        axios.post(gWebsiteURL + "report/get-Salary-class-year-data", formData, getAxiosConfigHeader())
            .then(function (response) {
                if (response.status == 200 && response.data != -1) {
                    $('#divSalaryGrid').html(response.data);

                    $('#tblCompensationClassYear').DataTable({
                        paging: false,
                        ordering: false,
                        bAutoWidth: false,
                        searching: false
                    });

                    if ($('.dvSalaryClassYear-wrapper').length == 0)
                        $('#tblCompensationClassYear').wrap("<div class='dvSalaryClassYear-wrapper'></div>");

                    $('.dvSalaryClassYear-wrapper').css('max-height', ($(window).height() * 0.75) + 'px');
                }

                setTimeout(function () {
                    $('.divInnerPopup').css('display', '');
                }, 2000)
            })
            .catch(function (error) {
                console.log(error);
                $('.divInnerPopup').css('display', '');
            });
    },

    sortElementData: function (element, count, col, sortOrder, sClass) {
        arrTotal = [];

        $(element).find('tr' + sClass + '.oriAttyRow').each(function () {
            arrTotal.push({
                '0': $(this).find('td:eq(0)').text(),
                'class': $(this).attr('class')
            });
        });

        var iRowNumber = 0;
        $(element).find('tr' + sClass + '.oriAttyRow').each(function () {
            $(this).find('.dataColumn').each(function () {
                var colIndex = $(this).index();
                if (colIndex == 1) {
                    console.log($(this).html());
                    arrTotal[iRowNumber][colIndex] = Date.parse(new Date($(this).html()));
                }
                else if (colIndex == 3 || colIndex == 2) {
                    arrTotal[iRowNumber][colIndex] = $(this).html();
                }
                else {
                    arrTotal[iRowNumber][colIndex] = isNumber($(this).html()) ? parseInt($(this).html().replace(/,/g, '')) : 0;
                }
            });
            iRowNumber++;
        });

        var arrTemp = common.sortAnArray(arrTotal, col, sortOrder);

        sClass = sClass.replace('.', '');

        $.each(arrTemp, function (index, value) {
            var $tr = $(element).find('tr:eq(' + count + ')');
            $tr.css({
                'display': 'table-row'
            }).removeAttr('class').addClass(sClass).addClass(value.class);

            $tr.find('td:eq(0)').html(value[0]);
            $tr.find('.dataColumn').each(function () {
                var colIndex = $(this).index();
                if (colIndex == 1) {
                    $(this).html(new Date(value[colIndex]).toLocaleDateString());
                }
                else if (colIndex == 3 || colIndex == 2) {
                    $(this).html(value[colIndex]);
                }
                else {
                    $(this).html(common.numberWithCommasDash(value[colIndex]))
                }
            });
            count++;
        })

        $('#tblCounselHistory tbody').find("[data-row='" + sClass + "']").find('i').removeClass('fa-plus').addClass('fa-minus');
    },

    manageCounselCompensationPlanningFixedHeader: function () {
        var secondRowColumnTop = $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(0) th:eq(5)').outerHeight(); //first row fifth column height
        $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(1) th').css('top', secondRowColumnTop).css('z-index', '1');

        //year end and bonus row
        var thirdRowColumnTop = secondRowColumnTop + $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(1) th.bonus-header').outerHeight(); //second row third column height
        $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(2) th').css('top', thirdRowColumnTop).css('z-index', '1');

        //starting annual row
        var fourthRowColumnTop = secondRowColumnTop + $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(1) th.sal-header').outerHeight(); //second row third column height
        $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(3) th').css('top', fourthRowColumnTop).css('z-index', '1');

        var fifthRowColumnTop = fourthRowColumnTop + $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(3) th:eq(1)').outerHeight(); //fourth row third column height
        $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(4) th').css('top', fifthRowColumnTop).css('z-index', '1');

        var totalRowColumnTop = $('#tblCounselCompensationPlanning thead:eq(0) tr:eq(0) th:eq(0)').outerHeight(); //First row first column height
        $('#tblCounselCompensationPlanning thead:eq(1) tr th').css('top', totalRowColumnTop);
    },

    deleteCounsel: function (id) {
        axios.get(gWebsiteURL + "report/delete-counsel-compensation", {
            params: {
                ID: id,
            },
            headers: {
                '__RequestVerificationToken': $("input[name=__RequestVerificationToken]").val()
            }
        }).then(response => {
            if (response.data != null) {
                Swal.fire(
                    'Deleted!',
                    'The record has been deleted.',
                    'success'
                );
                window.location.reload();

            } else {
                Swal.fire(
                    'Failed!',
                    'The record could not be deleted.',
                    'error'
                );
            }
        }).catch(error => {
            console.error('Error deleting record:', error);
            Swal.fire(
                'Error!',
                'There was a problem deleting the record.',
                'error'
            );
        });
    },

    checkForDuplicateCounsel: function (employeeName, id, obj) {
        try {
            var formData = new FormData();
            formData.append("EmployeeName", employeeName);
            formData.append("ID", parseInt(id));

            axios.post(gWebsiteURL + "report/check-duplicate-counsel-compensation", formData, getAxiosConfigHeader())
                .then(function (response) {
                    if (response.status == 200 && response.data != -1) {
                        let $row = $(obj).closest('td');
                        let isDuplicateField = $row.find('.hdnIsDuplicate');
                        if (response.data == "1") {
                            $row.find('.duplicate-employee').html('Counsel already exists!')
                            isDuplicateField.val('true');
                            $(obj).css('border-color', 'red');
                        } else {
                            $row.find('.duplicate-employee').html('')
                            isDuplicateField.val('false');
                        }
                    }
                    else {
                        Swal.fire("", "Internal server error...", "error");
                    }
                })
                .catch(function (error) {
                    console.log(error);
                });
        } catch (err) {
            console.log(err);
        }
    },

    // Save data to local storage
    saveToLocalStorage: function () {
        try {
            const data = ccp.collectFormData();
            const timestamp = new Date().toISOString();

            localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
            localStorage.setItem(LAST_SAVED_KEY, timestamp);

            // Show saving indicator
            this.showSavingIndicator();

            console.log('Data silently saved to local storage at: ' + timestamp);
        } catch (err) {
            console.error('Error saving to local storage:', err);
        }
    },

    // Collect current form data
    collectFormData: function () {
        const data = [];

        $('#tblCounselCompensationPlanning tbody tr.loopRow').each(function () {
            const $tr = $(this);
            const rowData = {
                ID: $tr.attr('data-id'),
                EmployeeCode: $tr.find('.EmployeeCode').html(),
                Name: $tr.find('.EmployeeName').text().trim(),
                // Collect all relevant input fields
                inputs: {}
            };

            // Collect text inputs in this row
            $tr.find('input[type="text"], textarea, select').each(function () {
                const $input = $(this);
                const inputName = $input.attr('name') || $input.attr('class').split(' ')[0];
                rowData.inputs[inputName] = $input.val();
            });

            data.push(rowData);
        });

        return {
            year: $('.year-tab.tab-selected').attr('data-year').trim(),
            rows: data,
            timestamp: new Date().toISOString()
        };
    },

    // Load data from local storage
    loadFromLocalStorage: function () {
        try {
            const savedData = localStorage.getItem(STORAGE_KEY);
            if (!savedData) {
                console.log('No saved data found in local storage');
                return false;
            }

            const data = JSON.parse(savedData);
            const currentYear = $('.year-tab.tab-selected').attr('data-year').trim();

            // Only load data if it's for the current year
            if (data.year !== currentYear) {
                console.log('Saved data is for a different year, not loading');
                return false;
            }

            // Apply the saved data to the form
            this.applyDataToForm(data);

            const lastSaved = localStorage.getItem(LAST_SAVED_KEY);
            console.log('Loaded data from local storage (last saved: ' + lastSaved + ')');

            // Show a notification to the user
            this.showRestoredNotification(lastSaved);

            return true;
        } catch (err) {
            console.error('Error loading from local storage:', err);
            return false;
        }
    },

    // Apply the loaded data to the form
    applyDataToForm: function (data) {
        data.rows.forEach(rowData => {
            const $row = $('#tblCounselCompensationPlanning tbody tr[data-id="' + rowData.ID + '"]');
            if ($row.length === 0) return;

            // Apply values to inputs
            for (const [inputName, value] of Object.entries(rowData.inputs)) {
                const $input = $row.find('.' + inputName);
                if ($input.length > 0) {
                    $input.val(value);

                    // Trigger change event for select elements and inputs that need formatting
                    if ($input.is('select') || $input.hasClass('number') ||
                        $input.hasClass('numberwithdecimal') || $input.hasClass('txtNextYearFTE')) {
                        $input.trigger('change');
                    }
                }
            }
        });

        // Recalculate totals and update header
        ccp.manageHeaderTotal();
    },

    // Show notification that data was restored
    showRestoredNotification: function (timestamp) {
        const date = new Date(timestamp);
        const formattedDate = date.toLocaleDateString() + ' ' + date.toLocaleTimeString();

        // Create notification element if it doesn't exist
        if ($('#autosaveNotification').length === 0) {
            $('body').append(`
                <div id="autosaveNotification" style="position:fixed; bottom:20px; right:20px; background:#f8f9fa; 
                     border:1px solid #dee2e6; padding:15px; border-radius:4px; box-shadow:0 2px 5px rgba(0,0,0,0.1); z-index:9999; display:none;">
                    <div style="margin-bottom:5px; font-weight:bold;">Draft restored</div>
                    <div id="autosaveTimestamp"></div>
                    <div style="margin-top:10px; display:flex; justify-content:space-between;">
                        <button id="keepChanges" class="btn btn-sm btn-primary">Keep changes</button>
                        <button id="discardChanges" class="btn btn-sm btn-secondary">Discard</button>
                    </div>
                </div>
            `);

            // Add event handlers for the buttons
            $('#keepChanges').on('click', function () {
                $('#autosaveNotification').fadeOut();
            });

            $('#discardChanges').on('click', function () {
                localStorage.removeItem(STORAGE_KEY);
                localStorage.removeItem(LAST_SAVED_KEY);
                $('#autosaveNotification').fadeOut();
                window.location.reload();
            });
        }

        // Update the timestamp and show the notification
        $('#autosaveTimestamp').text('Last edit was on ' + formattedDate);
        $('#autosaveNotification').fadeIn();
    },

    // Clear local storage data for this form
    clearLocalStorage: function () {
        localStorage.removeItem(STORAGE_KEY);
        localStorage.removeItem(LAST_SAVED_KEY);
        console.log('Local storage data cleared');
    },

    showSavingIndicator: function () {
        // Create the indicator if it doesn't exist
        if ($('#autosaveIndicator').length === 0) {
            $('body').append(`
            <div id="autosaveIndicator" style="position:fixed; bottom:10px; right:10px; 
                 background:rgba(0,0,0,0.7); color:white; padding:5px 10px; 
                 border-radius:3px; display:none; z-index:9999;">
                Saving...
            </div>
        `);
        }

        // Show the indicator and hide it after 1.5 seconds
        $('#autosaveIndicator').fadeIn().delay(1500).fadeOut();
    },
}

ccp.init();

