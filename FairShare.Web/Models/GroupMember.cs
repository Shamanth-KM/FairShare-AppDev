namespace FairShare.Web.Models
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; } = default!;
        public int UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
