function openCronUrl() {
	url = 'https://crontab.guru/#' + $('#cronExpression').val().split(' ').join('_');
	window.open(url, '_blank');
}