﻿using Grpc.Core;
using Microsoft.Extensions.Logging;
using Portal.ImageService.Data;
using Portal.ImageService.Protos;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portal.ImageService.Services
{
    public class UploadService : UploadFileService.UploadFileServiceBase
    {
        private readonly ILogger<UploadService> _logger;
        private readonly ImageDbContext _db;

        public UploadService(ILogger<UploadService> logger, ImageDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public override async Task<SendResult> SendFile(Chunk request, ServerCallContext context)
        {
            var content = request.Content.ToArray();
            var img = new Image
            {
                Id = new Guid(request.PostId),
                Full = content,
                TimeCreated = DateTime.Now
            };

            _db.Images.Add(img);
            await _db.SaveChangesAsync();

            //https://www.c-sharpcorner.com/article/publisher-and-consumer-with-rabbitmq-docker-container/
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = ConnectionFactory.DefaultUser,
                Password = ConnectionFactory.DefaultPass,
                Port = 5672
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();


            channel.QueueDeclare(queue: "image_crop",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            string msg = img.Id.ToString();
            var body = Encoding.UTF8.GetBytes(msg);

            channel.BasicPublish(exchange: "",
                 routingKey: "image_crop",
                 basicProperties: null,
                 body: body);

            return new SendResult { Success = true };
        }
    }
}
