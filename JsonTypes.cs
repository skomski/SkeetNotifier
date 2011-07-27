// ReSharper disable InconsistentNaming

namespace SkeetNotifier
{
    class JsonTypes
    {
        internal class Owner
        {
            public int user_id { get; set; }
            public string user_type { get; set; }
            public string display_name { get; set; }
            public int reputation { get; set; }
            public string email_hash { get; set; }
        }

        internal class Answer
        {
            public int answer_id { get; set; }
            public bool accepted { get; set; }
            public string answer_comments_url { get; set; }
            public int question_id { get; set; }
            public Owner owner { get; set; }
            public int creation_date { get; set; }
            public int last_edit_date { get; set; }
            public int last_activity_date { get; set; }
            public int up_vote_count { get; set; }
            public int down_vote_count { get; set; }
            public int view_count { get; set; }
            public int score { get; set; }
            public bool community_owned { get; set; }
            public string title { get; set; }
            public string body { get; set; }
        }

        internal class Root
        {
            public int total { get; set; }
            public int page { get; set; }
            public int pagesize { get; set; }
            public Answer[] answers { get; set; }
        }
    }
}
// ReSharper restore InconsistentNaming