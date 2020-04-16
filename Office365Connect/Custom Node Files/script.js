$(function() {
	toggleFolderNameState();
	$("#createFolder").click(toggleFolderNameState);
});

function toggleFolderNameState() {
	if (this.checked) {
		$("#foldername").removeAttr("disabled");
	} else {
		$("#foldername").attr("disabled", true);
	}
}