using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBGroupHelper.Model
{
    public class CommentsModel
    {
        public string post_id { get; set; }
        public string message { get; set; }
        public string reply_message { get; set; }
        public string updated_time { get; set; }
        public string id { get; set; }
        public DateTime created_time { get; set; }
        public bool replyed { get; set; }
    }
}
