



$(document).ready(function () {
	try {
		$('[data-toggle="tooltip"]').tooltip();
		$('.collapse').collapse();
		$('.modal').modal('hide');

		$(".draggable").draggable({ containment: "#notes-container", scroll: false, handle: ".sticky-note-header", cursor: "grabbing" });

		$(".fa-window-maximize").click(function () {
			var i = $(this).data("container");
			var f = i + "-heading";
			$("#" + i).animate({
				width: '400px',
				height: '300px'
			}, 500);
		});

		$(".fa-window-minimize").click(function () {
			var i = $(this).data("container");
			var f = i + "-heading";
			$("#" + i).animate({
				width: '170px',
				height: '100px'
			}, 500);
		});

		$(".fa-window-close").click(function () {
			var id = $(this).data("note-id");
			var t = $(this).data("container");
			if (confirm("Are you sure?")) {
				DeleteNote(id, t);
			}
		});


		//$('#UploadTaskAttachment').submit(function (e) {
		//	e.preventDefault();
		//	$("#btnSubmit").prop("disabled", "disabled");

		//	$.ajax({
		//		url: frm.action,
		//		type: frm.method,
		//		data: new FormData(this),
		//		cache: false,
		//		contentType: false,
		//		processData: false
		//	}).success(function (data) {
		//		DisplayAlert("Success", data.UploadedFileCount + " file(s) uploaded successfully.");
		//		console.log(data.UploadedFileCount + ' file(s) uploaded successfully.');
		//		$("#UploadTaskTAttachment").reset();
		//	}).error(function (xhr, error, status) {
		//		console.log(error, status);
		//		DisplayAlert("Error", "File upload failed.");
		//	}).complete(function () {
		//		$("#btnSubmit").removeProp("disabled");
		//	});

		//});

		if (document.getElementById('mips-graph-container') != null) {
			ConfigureMIPSGraph();
			ConfigureDwellTimeGraph();
			ConfigureRatesGraph();
		} else {
			ConfigureTasksCompletedGraph();
			ConfigureContactScoreGraph();
			ConfigureClinicScoreGraph();
		}

	}
	catch (e) {
		alert(e);
	}

});

/*
 *  public enum TaskTypes
    {
        RetrievalDatePassed = 1,
        SendRegisteredLetters = 2,
        ReviewPCPPreferences = 3,
        ScheduleRetrieval = 4,
        ReviewCase = 5,
        BuildCase = 6,
        ContactPCP = 7,
        PatientContactDue = 8
    }
 */

function EditPatientFilter(id) {
	window.location = "/Home/EditPatientFilter?id=" + id;
}

function ConfigureClinicScoreGraph() {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var resultdata = [];
	var resultdates = [];
	try {
		if (token != null && token.length > 0) {
			$.ajax({
				url: "/Home/ClinicSuccessRate",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							alert(data.Errors);
						}
					} else {
						resultdata = data.Scores;
						resultdates = data.Dates;
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
		try {
			var ctx = document.getElementById('graph3').getContext('2d');
			var chart = new Chart(ctx, {
				// The type of chart we want to create
				type: 'line',

				// The data for our dataset
				data: {
					labels: resultdates,
					datasets: [{
						label: 'Success Rate',
						fill: false,
						backgroundColor: 'rgb(255, 188, 96)',
						borderColor: 'rgb(255, 188, 96)',
						data: resultdata
					}]
				},

				// Configuration options go here
				options: {
					responsive: true,
					legend: {
						position: 'bottom',
						align: 'start'
					},
					title: {
						display: true,
						text: 'Clinic Retrieval Rate Success %'
					},
					scales: {
						yAxes: [{
							ticks: {
								suggestedMin: 0,
								suggestedMax: 1
							}
						}]
					}
				}
			});
		}
		catch (e) {
			alert(e);
		}
	}
	catch (e) {
		alert(e);
	}
}


function ConfigureContactScoreGraph() {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var resultdata = [];
	var resultdates = [];
	try {
		if (token != null && token.length > 0) {
			$.ajax({
				url: "/Home/PatientContactResults",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							alert(data.Errors);
						}
					} else {
						resultdata = data.Scores;
						resultdates = data.Dates;
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
		try {
			var ctx = document.getElementById('graph2').getContext('2d');
			var chart = new Chart(ctx, {
				// The type of chart we want to create
				type: 'line',

				// The data for our dataset
				data: {
					labels: resultdates,
					datasets: [{
						label: 'Success Rate',
						fill: false,
						backgroundColor: 'rgb(128, 188, 225)',
						borderColor: 'rgb(128, 188, 255)',
						data: resultdata
					}]
				},

				// Configuration options go here
				options: {
					responsive: true,
					legend: {
						position: 'bottom',
						align: 'start'
					},
					title: {
						display: true,
						text: 'Patient Contact Success Rate'
					},
					scales: {
						yAxes: [{
							ticks: {
								suggestedMin: 0,
								suggestedMax: 1
							}
						}]
					}
				}
			});
		}
		catch (e) {
			alert(e);
		}
	}
	catch (e) {
		alert(e);
	}
}


function ConfigureTasksCompletedGraph() {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var resultdata = [];
	try {
		if (token != null && token.length > 0) {
			$.ajax({
				url: "/Home/OpenTaskCounts",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							alert(data.Errors);
						}
					} else {
						resultdata = data.Counts;
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
		try {
			var ctx = document.getElementById('graph1').getContext('2d');
			var chart = new Chart(ctx, {
				// The type of chart we want to create
				type: 'horizontalBar',

				// The data for our dataset
				data: {
					labels: ['Build Case', 'Contact PCP', 'Patient Contact Due', 'Retrieval Date Passed', 'Schedule Retrieval', 'Send Reg. Letter'],
					datasets: [{
						label: 'Open Task Counts',
						fill: false,
						backgroundColor: 'rgba(180, 96, 128, 0.1)',
						borderColor: 'rgba(180, 64, 64, 0.65)',
						data: resultdata
					}]
				},

				// Configuration options go here
				options: {
					responsive: true,
					legend: {
						position: 'bottom',
						align: 'start'
					},
					title: {
						display: true,
						text: 'Open Task Counts'
					},
					elements: {
						rectangle: {
							backgroundColor: 'rgba(226, 96, 128, 0.1)',
							borderColor: 'rgba(200, 64, 64, 0.85)',
							borderWidth: 2
						}
					}
				}
			});
		}
		catch (e) {
			alert(e);
		}
	}
	catch (e) {
		alert(e);
	}
}

function ConfigureMIPSGraph(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	try {
		if (token != null && token.length > 0) {
			$.ajax({
				url: "/Home/MIPSHistory",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							alert(data.Errors);
						}
					} else {
						BuildMIPSGraph(data.PreviousYear, data.CurrentYear);

					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}


	}
	catch (e) {
		alert(e);
	}
}

function BuildMIPSGraph(prevYear, currYear) {
	var ctx = document.getElementById('mips-graph-container').getContext('2d');
	var chart = new Chart(ctx, {
		type: 'line',
		data: {
			labels: ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'],
			datasets: [{
				label: 'Previous Year',
				fill: false,
				backgroundColor: 'rgb(128, 225, 128)',
				borderColor: 'rgb(128, 225, 128)',
				data: prevYear
			}, {
				label: 'Current Year',
				fill: false,
				backgroundColor: '#3366ff',
				borderColor: '#3366ff',
				data: currYear
			}]
		},
		options: {
			legend: {
				position: 'bottom',
				align: 'start'
			},
			title: {
				display: true,
				text: 'MIPS History'
			},
			scales: {
				yAxes: [{
					ticks: {
						suggestedMin: 0,
						suggestedMax: 1
					}
				}]
			}
		}
	});
}


function ConfigureDwellTimeGraph(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	try {
		if (token != null && token.length > 0) {
			$.ajax({
				url: "/Home/FilterDwellTime",
				cache: false,
				method: "POST",
				dataType: "json",
				async: true,
				data: {
					__RequestVerificationToken: token
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							alert(data.Errors);
						}
					} else {
						BuildDwellTimeGraph(data.Avg, data.StdDev);
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}


	}
	catch (e) {
		alert(e);
	}
}

function BuildDwellTimeGraph(avg, stddev) {
	var ctx = document.getElementById('dwell-time-graph-container').getContext('2d');
	var chart = new Chart(ctx, {
		// The type of chart we want to create
		type: 'line',

		// The data for our dataset
		data: {
			labels: ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'],
			datasets: [{
				label: 'Std Dev',
				fill: false,
				backgroundColor: 'rgb(128, 225, 128)',
				borderColor: 'rgb(128, 225, 128)',
				data: stddev
			}, {
				label: 'Average',
				fill: false,
				backgroundColor: '#3366ff',
				borderColor: '#3366ff',
				data: avg
			}]
		},

		// Configuration options go here
		options: {
			legend: {
				position: 'bottom',
				align: 'start'
			},
			title: {
				display: true,
				text: 'Filter DwellTime'
			},
			scales: {
				yAxes: [{
					ticks: {
						suggestedMin: 0,
						suggestedMax: 360
					}
				}]
			}
		}
	});

}


function ConfigureRatesGraph(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var avg = [], oa = [];
	try {
		if (token != null && token.length > 0) {
			$.ajax({
				url: "/Home/RetrievalRates",
				cache: false,
				method: "POST",
				dataType: "json",
				async: true,
				data: {
					__RequestVerificationToken: token
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							alert(data.Errors);
						}
					} else {
						avg = data.Avg;
						oa = data.Overall;

						BuildRateChart(avg, oa);
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
	}
	catch (e) {
		alert(e);
	}
}

function BuildRateChart(avg, oa) {
	var ctx = document.getElementById('rates-graph-container').getContext('2d');
	var chart = new Chart(ctx, {
		// The type of chart we want to create
		type: 'line',
		// The data for our dataset
		data: {
			labels: ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'],
			datasets: [{
				label: 'This Year',
				fill: false,
				backgroundColor: '#3366ff',
				borderColor: '#3366ff',
				data: avg
			}]
		},
		options: {
			legend: {
				position: 'bottom',
				align: 'start'
			},
			title: {
				display: true,
				text: 'Retrieval Rates'
			},
			scales: {
				yAxes: [{
					ticks: {
						suggestedMin: 0,
						suggestedMax: 1
					}
				}]
			},
			annotation: {
				annotations: [{
					type: 'line',
					id: 'hLine',
					mode: 'horizontal',
					scaleID: 'y-axis-0',
					value: oa,  // data-value at which the line is drawn
					borderWidth: 2,
					borderColor: 'red',
					label: {
						backgroundColor: "red",
						content: "All Time",
						enabled: true
					}

				}]
			}
		}
	});
}


function NewNote() {
	$('#qn-dialog').modal('show');
}

function SaveNote() {
	var c = $(".sticky-note-sm").length;

	if (c >= 9) {
		alert("You may only have 10 Quick Notes at any one time.  Please delete one and try again.");
		return;
	}

	var data = {
		Heading: $("#qn-heading").val(),
		Body: $("#qn-body").val()
	};

	try {
		$.ajax({
			url: "/Home/SaveNote",
			cache: false,
			method: "POST",
			dataType: "html",
			async: false,
			data: data
		}).done(function (result) {
			$("#notes-container").append(result);
		});

		$(".fa-window-maximize").click(function () {
			var i = $(this).data("container");
			var f = i + "-heading";
			$("#" + i).animate({
				width: '400px',
				height: '300px'
			}, 500);

		});

		$(".fa-window-minimize").click(function () {
			var i = $(this).data("container");
			//var f = i + "-heading";
			$("#" + i).animate({
				width: '170px',
				height: '100px'
			}, 500);
		});

		$(".fa-window-close").click(function () {
			var id = $(this).data("note-id");
			var t = $(this).data("container");
			if (confirm("Are you sure?")) {
				DeleteNote(id, t);
			}
		});
	}
	catch (e) {
		alert(e);
	}
}

function EditNote(id, identifier, heading, body) {
	var h, b;

	h = $('#' + heading).val();
	b = $('#' + body).val();

	var data = {
		id: id,
		heading: h,
		body: b
	};

	try {
		$.ajax({
			url: "/Home/EditNote",
			cache: false,
			method: "POST",
			dataType: "html",
			async: false,
			data: data
		}).done(function (result) {
			$("#" + identifier).animate({
				width: '170px',
				height: '100px'
			}, 500);
		});


	}
	catch (e) {
		alert(e);
	}
}

function DeleteNote(id, t) {
	var returned = false;
	var data = { noteId: id };
	$.ajax({
		url: "/Home/DeleteNote",
		cache: false,
		method: "POST",
		dataType: "json",
		async: true,
		data: data
	}).done(function (result) {
		if (result == "Ok") {
			var tgt = $("#" + t);
			tgt.remove();
		}
	});

	return returned;
}

function flipit(el) {

	var target = $(el);

	if (Modernizr.csstransitions) {
		target.css({
			"transition": "all 200ms ease-in-out"
		});
	}

	var rot = target.data('rotation');

	if (rot == '0') {
		rot = 180;
		target.data('rotation', '180');
	} else {
		rot = 0;
		target.data('rotation', '0');
	}
	if (Modernizr.csstransitions) {
		target.css({ "transform": "rotate(" + rot + "deg)" });
	} else {
		target.stop().animate(
			{ rotation: rot },
			{
				duration: 200,
				step: function (now, fx) {
					$(this).css({ "transform": "rotate(" + now + "deg)" });
				}
			}
		);
	}
}

function DisplayTaskEditor(taskId, typeid) {
	$("#TaskId").val(taskId);
	var result_data;

	ClearAlert();

	$('#modal-body').html("");

	if (typeid == 1) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			return CompleteRetrievalOverdueTask(taskId);
		}).html("Save and Complete");
	}
	else if (typeid == 2) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			return CompleteSendRegisteredLettersTask(taskId);
		}).html("Save and Complete");
	}
	else if (typeid == 4) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			return CompleteScheduleRetrievalTask(taskId);
		}).html("Save and Complete");
	}
	else if (typeid == 5) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			return CompleteReviewCaseTask(taskId);
		});
	} else if (typeid == 6) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			$('#spTaskId').val(taskId);
			var contact_success = $('#result-code').val() == '1';
			if (SaveChanges()) {
				if (contact_success) {
					$('#select-physician-dialog').modal('show');
				} else {
					return CompleteBuildCaseTask(taskId);
				}
			}
		}).html("Save and Complete");
	} else if (typeid == 7) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			return CompletePCPContactTask(taskId);
		}).html("Save and Complete");
	} else if (typeid == 8) {
		//$('#save').hide();
		$('#complete').off('click').on('click', function () {
			return CompletePatientContactDueTask(taskId);
		}).html("Save and Complete");
	}

	var jqxhr = $.ajax(
		{
			url: "/Home/GetTaskEditor",
			cache: false,
			method: "GET",
			dataType: "html",
			async: false,
			traditional: true,
			data: { TaskId: taskId }
		})
		.done(function (result) {
			result_data = result;
		})
		.fail(function (request, status, error) {
			alert(request.responseText);
		});

	$("#ModalTitle").html(GetTaskTitle(typeid));
	$('#modal-body').html(result_data);
	if (typeid == 5) {
		SetupReviewCaseTables();
	}
	$('#task-dialog').modal('show');

}

function GetTaskTitle(t) {
	var returned = "";

	switch (t) {
		case 1:
			returned = "Retrieval Date Passed";
			break;
		case 2:
			returned = "Send Registered Letters";
			break;
		case 3:
			returned = "Review PCP Preferences";
			break;
		case 4:
			returned = "Schedule Retrieval";
			break;
		case 5:
			returned = "Review Case";
			break;
		case 6:
			returned = "Build Case";
			break;
		case 7:
			returned = "Contact PCP";
			break;
		case 8:
			returned = "Contact Patient";
			break;
		default:
			returned = "Undefined Task Type";
			break;
	}

	return returned;
}

//function SaveChanges(amCompleting) {
function SaveChanges() {
	var result_data;
	var task_id = $("#TaskId").val();
	var patient_questions = $('.question-response-patient');
	var physician_questions = $('.question-response-physician');
	var patient_question_responses = [];
	var physician_question_responses = [];
	var rc = $('#result-code').val();
	var ct = $('#contact-type').val();
	var n = $('#contact-note').val();
	var rcp = $('#physician-result-code').val();
	var ctp = $('#physician-contact-type').val();
	var np = $('#physician-contact-note').val();
	var returned = false;

	$('#contact-type').removeClass('form-control-invalid');
	$('#result-code').removeClass('form-control-invalid');

	var c = true;
	if (ct == null || ct == '') {
		$('#contact-type').addClass('form-control-invalid');
		c = false;
	}

	if (rc == null || rc == '') {
		$('#result-code').addClass('form-control-invalid');
		c = false;
	}

	if (!c) {
		DisplayAlert('Error:', 'Please provide contact information.');
		return false;
	}


	$.each(patient_questions, function (i, v) {
		var inserted = new Object();
		inserted.QuestionId = v.getAttribute("data-question-id");
		inserted.Response = v.value;
		patient_question_responses.push(inserted);
	});

	$.each(physician_questions, function (i, v) {
		var inserted = new Object();
		inserted.QuestionId = v.getAttribute("data-question-id");
		inserted.Response = v.value;
		physician_question_responses.push(inserted);
	});

	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	$('#save').prop("disabled", true);
	var jqxhr = $.ajax(
		{
			url: "/Home/SaveBuildCaseTask",
			cache: false,
			method: "POST",
			dataType: "json",
			async: false,
			data: {
				__RequestVerificationToken: token,
				TaskId: task_id,
				PatientQuestionResponses: patient_question_responses,
				PhysicianQuestionResponses: physician_question_responses,
				ResultCodeId: rc,
				ContactTypeId: ct,
				Note: n,
				PhysicianContactNote: np,
				PhysicianContactResultCodeId: rcp,
				PhysicianContactTypeId: ctp
			}
		})
		.done(function (result) {
			result_data = result;
			if (result_data.Success == 'y') {
				//$('#result-code option:eq(0)').attr('selected', 'selected');
				//$('#contact_type option:eq(0)').attr('selected', 'selected');
				//$('#physician-result-code option:eq(0)').attr('selected', 'selected');
				//$('#physician-contact-type option:eq(0)').attr('selected', 'selected');

				//$('#contact-note').val('');
				//$('#physician-contact-note').val('');
				returned = true;
				//DisplayAlert("Success", "Save Successful.");
			} else {
				DisplayAlert("Error", result_data.Message);
			}
			//$('#save').prop("disabled", false);
		})
		.fail(function (request, status, error) {
			//$('#save').prop("disabled", false);
			DisplayAlert("Error", "Failed to save task changes, please contact tech support.");
		});


	return returned;
}

function UploadFile() {
	document.getElementById("btnSubmit").disabled = true;

	var task_dialog = $("#task-dialog");
	var csr = task_dialog.css('cursor');
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();

	var fd = new FormData();
	fd.append("pdf", document.getElementById("upload").files[0]);
	fd.append("__RequestVerificationToken", token);
	fd.append("taskId", document.getElementById("taskId").value);

	try {
		task_dialog.css('cursor', 'wait');

		$.ajax({
			url: '/Home/UploadTaskAttachment',
			cache: false,
			method: "POST",
			dataType: "json",
			contentType: false,
			processData: false,
			async: true,
			data: fd,
			timeout: 1000 * 60 * 10,	// 10 minutes is excessive
			success: function (data) {
				var msg;
				if (data.Success != 'true') {
					if (data.Errors != null) {
						DisplayAlert("Error: ", data.Errors);
					}
				} else {
					var t = $('#files-table');
					$('#tr-placeholder', t).remove();
					t.append("<tr><td align='left'>" + data.FileName + "</td><td align='left'>" + data.DateUploaded + "</td><td align='left'>" + data.FileSize + "</td><td>&nbsp;</td>");
					DisplayAlert("Success: ", data.FileName + " uploaded successfully.");
				}
			},
			error: function (xhr, error, status) {
				if (xhr.responseText != null && xhr.responseText.length > 0)
					alert(xhr.responseText);
				else
					alert(error + "::" + status);
			},
			complete: function () {
				$("#upload").val("");
				document.getElementById("btnSubmit").disabled = false;
			}
		});
	}
	catch (e) {
		alert(e);
	}

	task_dialog.css("cursor", csr);
}

function DisplayAlert(heading, msg) {
	var h = "<div class='alert alert-info alert-dismissible border-danger fade show' role='alert'><span id='alert-title' style='font-weight: bold; margin-right: 5px;'>" + heading + "</span><span id='alert-message'>" + msg + "</span><button type='button' class='close' data-dismiss='alert' aria-label='Close'><span aria-hidden='true'>&times;</span></button></div>";
	var tgt = $("#alert-container");
	tgt.html(h);
}

function ClearAlert() {
	$("#alert-container").html('');
}

function CompleteBuildCaseTask(id, antecedentTaskTargetUserId) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();

	if (token != null && token.length > 0) {
		//if (SaveChanges(true)) {
			$.ajax({
				url: "/Home/CompleteTask",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token,
					taskId: id,
					antecedentTaskTargetUserId: antecedentTaskTargetUserId
				},
				success: function (data) {
					try {
						if (!data.Success) {
							DisplayAlert("Error", data.Errors[0]);
						} else {
							$('#TR-' + id).remove();
							$('.modal').modal('hide');
						}
					}
					catch (e) {
						alert(e);
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						DisplayAlert("Error", xhr.responseText);
					else
						DisplayAlert("Error", error);
				}
			});
		//}
	}
}

function SetupReviewCaseTables() {
	try {
		var table = $('#contacts-table').DataTable({
			"pagingType": "first_last_numbers",
			responsive: true
		});

		var info = table.page.info();
		if (info.pages <= 1) {
			$("#contacts-table_paginate").hide('fast');
		}

		var table2 = $('#attachments-table').DataTable({
			"pagingType": "first_last_numbers",
			responsive: true
		});

		var info2 = table2.page.info();
		if (info2.pages <= 1) {
			$("#attachments-table_paginate").hide('fast');
		}
	}
	catch (e) {
		alert(e);
	}
}

function CompleteReviewCaseTask(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();

	var action = $("input:radio[name='Action']:checked").val();

	if (action == null || action == undefined) {
		alert('Please select a result before completing the task.');
		return;
	}

	var rd = $('#reassess-days').val();
	var cr = $('#pcp-note').val();
	var over = 'off';
	var pn = $('#perm-note').val();
	var srn = $('#sr-note').val();

	if ($('#chk-override').prop('checked') == true)
		over = 'on';

	if (token != null && token.length > 0) {
		$.ajax({
			url: "/Home/CompleteTask",
			cache: false,
			method: "POST",
			dataType: "json",
			async: false,
			data: {
				__RequestVerificationToken: token,
				taskId: id,
				Action: action,
				ReassessDays: rd,
				ContactReason: cr,
				Override: over,
				MakePermanentNote: pn,
				ScheduleRetrievalNote: srn
			},
			success: function (data) {
				if (!data.Success) {
					if (data.Errors != null) {
						DisplayAlert("Error", data.Errors[0]);
					}
				} else {
					$('#TR-' + id).remove();
					$('.modal').modal('hide');
				}
			},
			error: function (xhr, error, status) {
				if (xhr.responseText != null && xhr.responseText.length > 0)
					alert(xhr.responseText);
				else
					alert(error + "::" + status);
			}
		});
	}
}

function CompleteRetrievalOverdueTask(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var trdate = $('#TargetRemovalDate').val();
	var rc = $('#result-code').val();
	var ct = $('#contact-type').val();
	var n = $('#contact-note').val();

	if (rc == '1') {
		if (trdate == null || trdate.length == 0) {
			if (!Confirm("Contact was successful, but no removal date was provided.  Continue saving only the contact attempt?")) {
				return;
			}
		}
	}

	if (token != null && token.length > 0) {
		$.ajax({
			url: "/Home/CompleteTask",
			cache: false,
			method: "POST",
			dataType: "json",
			async: false,
			data: {
				__RequestVerificationToken: token,
				taskId: id,
				TargetRetrievalDate: trdate,
				Note: n,
				ResultCodeId: rc,
				ContactTypeId: ct
			},
			success: function (data) {
				if (!data.Success) {
					if (data.Errors != null) {
						DisplayAlert("Error", data.Errors[0]);
					}
				} else {
					$('#TR-' + id).remove();
					$('.modal').modal('hide');
				}
			},
			error: function (xhr, error, status) {
				if (xhr.responseText != null && xhr.responseText.length > 0)
					alert(xhr.responseText);
				else
					alert(error + "::" + status);
			}
		});
	}
}

function CompleteSendRegisteredLettersTask(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var sd = $('#send-date').val();
	var tn = $('#tracking-number').val();
	var n = $('#note').val();

	if (token != null && token.length > 0) {
		$.ajax({
			url: "/Home/CompleteTask",
			cache: false,
			method: "POST",
			dataType: "json",
			async: false,
			data: {
				__RequestVerificationToken: token,
				taskId: id,
				SendDate: sd,
				TrackingNumber: tn,
				Note: n
			},
			success: function (data) {
				if (!data.Success) {
					if (data.Errors != null) {
						DisplayAlert("Error", data.Errors[0]);
					}
				} else {
					$('#TR-' + id).remove();
					$('.modal').modal('hide');
				}
			},
			error: function (xhr, error, status) {
				if (xhr.responseText != null && xhr.responseText.length > 0)
					alert(xhr.responseText);
				else
					alert(error + "::" + status);
			}
		});
	}
}

function CompletePatientContactDueTask(id) {

	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var rc = $('#result-code').val();
	var ct = $('#contact-type').val();
	var n = $('#contact-note').val();

	if (token != null && token.length > 0) {
		try {
			$.ajax({
				url: "/Home/CompleteTask",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token,
					taskId: id,
					ResultCodeId: rc,
					ContactTypeId: ct,
					Note: n
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							DisplayAlert("Error", data.Errors[0]);
						}
					} else {
						$('#TR-' + id).remove();
						$('.modal').modal('hide');
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
		catch (e) {
			alert(e);
		}
	}
}

function CompletePCPContactTask(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var pa = $('#PCPApproved').prop('checked');
	var ct = $("#contact-type").val();
	var rc = $("#result-code").val();
	var cn = $('#contact-note').val();

	if (token != null && token.length > 0) {
		$.ajax({
			url: "/Home/CompleteTask",
			cache: false,
			method: "POST",
			dataType: "json",
			async: false,
			data: {
				__RequestVerificationToken: token,
				taskId: id,
				ContactNote: cn,
				RemovalApproval: pa,
				ContactType: ct,
				ResultCode: rc
			},
			success: function (data) {
				if (!data.Success) {
					if (data.Errors != null) {
						DisplayAlert("Error", data.Errors[0]);
					}
				} else {
					$('#TR-' + id).remove();
					$('.modal').modal('hide');
				}
			},
			error: function (xhr, error, status) {
				if (xhr.responseText != null && xhr.responseText.length > 0)
					alert(xhr.responseText);
				else
					alert(error + "::" + status);
			}
		});
	}
}

function CompleteScheduleRetrievalTask(id) {

	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();
	var trd = $('#target-removal-date').val();
	var rc = $('#result-code').val();
	var ct = $('#contact-type').val();
	var n = $('#contact-note').val();

	if (token != null && token.length > 0) {

		$('#contact-type').removeClass('form-control-invalid');
		$('#result-code').removeClass('form-control-invalid');

		var c = true;
		if (ct == null || ct == '') {
			$('#contact-type').addClass('form-control-invalid');
			c = false;
		}

		if (rc == null || rc == '') {
			$('#result-code').addClass('form-control-invalid');
			c = false;
		}

		if (!c) {
			DisplayAlert('Error:', 'Please provide contact information.');
			return;
		} else {
			if (rc == '1') {
				if (trd == null || trd.length == 0) {
					$('#target-removal-date').addClass('form-control-invalid');
					DisplayAlert('Error:', 'You must supply a target removal date for a successful contact.');
					return;
				}
			}
		}

		try {
			$.ajax({
				url: "/Home/CompleteTask",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token,
					taskId: id,
					TargetRetrievalDate: trd,
					ResultCodeId: rc,
					ContactTypeId: ct,
					Note: n
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Errors != null) {
							DisplayAlert("Error", data.Errors[0]);
						}
					} else {
						$('#TR-' + id).remove();
						$('.modal').modal('hide');
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
		catch (e) {
			alert(e);
		}
	}
}

function CompleteReviewPCPPreferencesTask(id) {
}

function ClaimTask(id, typeid) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();

	if (token != null && token.length > 0) {
		try {
			$.ajax({
				url: "/Home/ClaimTask",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token,
					taskId: id
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Message != null) {
							alert(data.Message);
						}
					} else {
						$('#claim-' + id).remove();
						$('#edit-' + id).show();
						$('.modal').modal('hide');
						DisplayTaskEditor(id, typeid);
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						alert(xhr.responseText);
					else
						alert(error + "::" + status);
				}
			});
		}
		catch (e) {
			alert(e);
		}
	}
}

function UnclaimTask(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();

	if (token != null && token.length > 0) {
		try {
			$.ajax({
				url: "/Home/UnclaimTask",
				cache: false,
				method: "POST",
				dataType: "json",
				async: false,
				data: {
					__RequestVerificationToken: token,
					taskId: id
				},
				success: function (data) {
					if (!data.Success) {
						if (data.Message != null) {
							DisplayAlert(data.Message);
						}
					} else {
						$('#unclaim-' + id).hide();
						$('#edit-'+id).hide();
						$('#claim-' + id).show();
					}
				},
				error: function (xhr, error, status) {
					if (xhr.responseText != null && xhr.responseText.length > 0)
						DisplayAlert(xhr.responseText);
					else
						DisplayAlert(error + "::" + status);
				}
			});
		}
		catch (e) {
			DisplayAlert(e);
		}
	}
}

function DeleteAttachment(id) {
	var form = $('#__AjaxAntiForgeryForm');
	var token = $('input[name="__RequestVerificationToken"]', form).val();

	if (confirm("Are you sure?  This will remove the attachment from any other tasks that the file was attached to.")) {
		if (token != null && token.length > 0) {
			try {
				$.ajax({
					url: "/Home/DeleteUpload",
					cache: false,
					method: "GET",
					dataType: "json",
					async: false,
					data: {
						__RequestVerificationToken: token,
						taskAttachmentId: id
					},
					success: function (data) {
						if (!data.Success) {
							DisplayAlert(data.Error);
						} else {
							$("#task-attachment-" + id).remove();
							DisplayAlert("Success", "Attachment was deleted.");
						}
					},
					error: function (xhr, error, status) {
						if (xhr.responseText != null && xhr.responseText.length > 0)
							alert(xhr.responseText);
						else
							alert(error + "::" + status);
					}
				});
			}
			catch (e) {
				alert(e);
			}
		}
	}
}

