using QA_Infrastructure;

namespace QA.Business.Message
{
    public class NeedLoginMessage
    {
        public EN_UserType TargetUserType { get; set; } = EN_UserType.User;
    }
}
