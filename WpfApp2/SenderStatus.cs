namespace PacketCannon
{
    public static class SenderStatus
    {
        public enum SenderStat
        {
            SendSyn,
            WaitingForAck,
            SendingAck,
            SendingSlowLorisGetHeader,
            SendingKeepAliveForSlowLoris,
            SedingSlowPostHeader,
            SedingKeepAliveForSlowPost,
            SedingGetForSlowRead,
            RecievingSlowRead,
            SendKeepAliveAckForSlowRead
        }
    }
}