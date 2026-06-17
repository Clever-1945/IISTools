using IISTools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISTools.Helpers
{
    public class IisInfoHelper
    {
        public enum IisRunState
        {
            None,
            RUNNING,
            STOPPED
        }
        
        private CmdHelper _cmdHelper = new CmdHelper();

        public async Task Stop()
        {
            var result = _cmdHelper.Run("sc stop w3svc");
            return;
        }

        public async Task Start()
        {
            var result = _cmdHelper.Run("sc start w3svc");
            return;
        }

        public async Task<IisRunState> GetStatus()
        {
            var result = _cmdHelper.Run("chcp 437 >nul && sc query w3svc");
            if (result == null)
                return IisRunState.None;

            if (result.IsError)
                return IisRunState.None;

            if (String.IsNullOrWhiteSpace(result.Output))
                return IisRunState.None;

            var list = result.Output.Split(new char[] { '\r', '\n' }).Select(x => x.Trim()).ToArray();
            foreach ( var line in list)
            {
                if(line.StartsWith("STATE", StringComparison.OrdinalIgnoreCase))
                {
                    var index = line.LastIndexOf(' ');
                    if (index > 0)
                    {
                        var text = line.Substring(index);
                        var value = text.ToEnumOrNull<IisRunState>();
                        if (value != null)
                        {
                            return value.Value;
                        }
                    }
                }
            }

            return IisRunState.None;
        }
    }
}
