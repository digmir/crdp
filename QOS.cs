using System;
using System.Collections.Generic;
using System.Text;

namespace rdpcommon
{
    class QOS
    {
        int m_limit_in = 128 * 1024;
        int m_limit_out = 128 * 1024;

        double m_stat_in = 0;
        double m_stat_out = 0;

        double m_total_in = 0;
        double m_total_out = 0;

        DateTime m_begin_time = DateTime.Now;

        public void SetQOS(int limit_in, int limit_out)
        {
            m_limit_in = limit_in;
            m_limit_out = limit_out;
            m_stat_in = 0;
            m_stat_out = 0;
            m_begin_time = DateTime.Now;
        }

        private void stat()
        {
            try
            {
                double ts = (DateTime.Now - m_begin_time).TotalMilliseconds;
                if (ts < 1)
                {
                    System.Threading.Thread.Sleep(1);
                    return;
                }

                double inss = (m_stat_in / (ts / 1000)) - m_limit_in;
                double outss = (m_stat_out / (ts / 1000)) - m_limit_out;

                if (inss > 0)
                {
                    System.Threading.Thread.Sleep((int)((inss / m_limit_in) * 100));
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }

                if (outss > 0)
                {
                    System.Threading.Thread.Sleep((int)((outss / m_limit_out) * 100));
                }

                if (ts > 3000)
                {
                    m_stat_in = 0;
                    m_stat_out = 0;
                    m_begin_time = DateTime.Now;
                }
            }catch
            {

            }
        }

        public void DataIn(int len)
        {
            m_total_in += len;
            m_stat_in += len;
            stat();
        }

        public void DataOut(int len)
        {
            m_total_out += len;
            m_stat_out += len;
            stat();
        }
    }
}
