using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class EducationTopic
    {
        public string Title { get; set; }
        public string FileName { get; set; }
        public List<string> Images { get; set; }
        public string QuizTitle { get; set; }
        public QuizData Quiz { get; set; }
    }
    public class QuizData
    {
        public string Title { get; set; }
        public List<QuizOption> Questions { get; set; }
    }
    public class QuizOption
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}
