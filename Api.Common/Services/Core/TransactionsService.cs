using Api.Data.Models.Core;
using AutoMapper;
using Api.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Api.Data.Repositories.Core;
using Api.Common.Models.DTOs.Core;

namespace Api.Common.Services.Core
{
    public interface ITransactionsService
    {
        Task<List<TransactionDTO>> GetAllTransactions();
    }

    public class TransactionsService : ITransactionsService
    {
        //private readonly EndpointDataSource _endpointDataSource;
        private readonly IModuleRepository _moduleRepository;
        private readonly ICrmApplicationRepository _applicationRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public TransactionsService(/*EndpointDataSource endpointDataSource,*/
            IModuleRepository moduleRepository, ICrmApplicationRepository applicationRepository,
            ITransactionRepository transactionRepository, IMapper mapper,
            ILogger<TransactionsService> logger)
        {
            //_endpointDataSource = endpointDataSource;
            _moduleRepository = moduleRepository;
            _applicationRepository = applicationRepository;
            _transactionRepository = transactionRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<TransactionDTO>> GetAllTransactions()
        {
            return _mapper.Map<List<TransactionDTO>>(await _transactionRepository.GetAll());
        }
    }
}
