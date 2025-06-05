const pollOptions = document.querySelectorAll('.poll-option');
const progressBars = document.querySelectorAll('.poll-progress');

setInterval(() => {
    const randomOption = Math.floor(Math.random() * pollOptions.length);
    const currentWidth = parseInt(progressBars[randomOption].style.width) || 0;
    const newWidth = Math.min(currentWidth + Math.random() * 5, 100);
    progressBars[randomOption].style.width = newWidth + '%';
}, 3000);

pollOptions.forEach((option, index) => {
    option.addEventListener('click', () => {
        pollOptions.forEach(opt => opt.classList.remove('selected'));
        option.classList.add('selected');
    });
});