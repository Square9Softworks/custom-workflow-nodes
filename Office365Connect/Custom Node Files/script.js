$(function() {
	toggleFolderNameState();
	$("#createFolder").click(toggleFolderNameState);
});

function toggleFolderNameState() {
	if ($("#createFolder").is(":checked")) {
		$("#foldername").removeAttr("disabled");
	} else {
		$("#foldername").attr("disabled", true);
	}
}