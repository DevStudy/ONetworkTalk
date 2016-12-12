﻿using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using ONetworkTalk.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Handler
{
    public class ChannelServerHandler : ChannelHandlerAdapter
    {
        WorkerEngine<RequestInfo> workerEngine;

        public UserManager UserManager { get; private set; }

        public ChannelServerHandler(UserManager userManager)
        {
            this.UserManager = userManager;
        }

        public ChannelServerHandler(UserManager userManager, WorkerEngine<RequestInfo> workerEngine)
        {
            this.UserManager = userManager;
            this.workerEngine = workerEngine;
            this.workerEngine.Start();
        }

        public IByteBuffer AcceptInboundMessage(object msg)
        {
            return msg as IByteBuffer;
        }

        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            var buffer = AcceptInboundMessage(msg);
            try
            {
                if (buffer != null)
                {
                    MessagePacket message = new MessagePacket(buffer);
                    this.workerEngine.Add(new RequestInfo(ctx.Channel, ctx, message));
                }
                else
                {
                    ctx.FireChannelRead(msg);
                }
            }
            finally
            {
                ReferenceCountUtil.Release(buffer);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext contex)
        {
            contex.Flush();
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            var eventState = evt as IdleStateEvent;
            if (eventState != null && eventState.State == IdleState.ReaderIdle)
            {
                context.Channel.CloseAsync();
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            //Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            UserManager.Remove(context.Channel);
        }

        public override bool IsSharable => true;
    }
}
