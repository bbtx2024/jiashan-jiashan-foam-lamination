using QA.Business.Define;
namespace QA.Business.Message
{
    public class TapeUseCountChangeMessage
    {
        FeederId _id;
        public FeederId ID
        {
            get => _id;
            set
            {
                _id = value;
            }
        }
    }
}
