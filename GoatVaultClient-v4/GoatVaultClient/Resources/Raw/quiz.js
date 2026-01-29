function renderQuiz() {
    if (!window.currentQuiz) return;

    var container = document.getElementById('dynamic-quiz-container');
    if (!container) return;

    // Notice we use single quotes for attributes: class='...' 
    // OR double quotes: class="..." (but NOT class=""..."")
    var html = '<div class="quiz-header">📝 Knowledge Check</div>';

    html += '<div class="quiz-body">';
    html += '<p class="question">' + window.currentQuiz.title + '</p>';

    window.currentQuiz.questions.forEach((opt, index) => {
        html += '<button class="quiz-btn" onclick="checkDynamicAnswer(this, ' + index + ')">';
        html += '<span class="btn-text">' + opt.text + '</span>';
        html += '<span class="icon"></span>';
        html += '</button>';
    });

    html += '<div id="quiz-result" class="result-box"></div>';
    html += '</div>';

    container.innerHTML = html;
}

function resetQuiz() {
    var allBtns = document.getElementsByClassName('quiz-btn');
    for (var i = 0; i < allBtns.length; i++) {
        allBtns[i].disabled = false;
        allBtns[i].classList.remove('disabled', 'correct', 'wrong');
    }

    var resultBox = document.getElementById('quiz-result');
    resultBox.classList.remove('show-result', 'success', 'error');
    resultBox.innerHTML = '';
}

function checkDynamicAnswer(btn, index) {
    var allBtns = document.getElementsByClassName('quiz-btn');
    for (var i = 0; i < allBtns.length; i++) {
        allBtns[i].disabled = true;
        allBtns[i].classList.add('disabled');
    }

    var isCorrect = window.currentQuiz.questions[index].isCorrect;
    var resultBox = document.getElementById('quiz-result');

    if (isCorrect) {
        btn.classList.add('correct');
        btn.classList.remove('disabled');

        resultBox.innerHTML = '<strong>🎉 Correct!</strong> Great job.';
        resultBox.classList.add('show-result', 'success');

        setTimeout(() => {
            window.location.href = 'goatvault://quiz_complete?success=true';
        }, 1500);
    } else {
        btn.classList.add('wrong');
        btn.classList.remove('disabled');

        resultBox.innerHTML = '<strong>❌ Incorrect.</strong><br><button class="retry-btn" onclick="resetQuiz()">🔄 Try Again</button>';
        resultBox.classList.add('show-result', 'error');
    }
}

window.onload = renderQuiz;