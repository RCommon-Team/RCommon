using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.Application.DTO;
using RCommon.DataServices.Transactions;
using RCommon.Domain.DomainServices;
using RCommon.Domain.Repositories;
using RCommon.ExceptionHandling;
using RCommon.Validation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Domain.DomainServices
{
    public  class CrudDomainService<TEntity> : RCommonDomainService, ICrudDomainService<TEntity>
        where TEntity : class
    {
        private IEntityValidator<TEntity> _entityValidator;
        private IBusinessRulesEvaluator<TEntity> _businessRulesEvaluator;
        private readonly IUnitOfWorkScopeFactory _unitOfWorkScopeFactory;
        private readonly IEagerFetchingRepository<TEntity> _repository;

        public CrudDomainService(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IEagerFetchingRepository<TEntity> repository, ILogger logger, IExceptionManager exceptionManager)
            : base(logger, exceptionManager)
        {
            this._unitOfWorkScopeFactory = unitOfWorkScopeFactory;
            this._repository = repository;
        }

        protected virtual void SetBusinessRulesEvaluator(IBusinessRulesEvaluator<TEntity> businessRulesEvaluator)
        {
            this._businessRulesEvaluator = businessRulesEvaluator;
        }

        protected virtual void SetEntityValidator(IEntityValidator<TEntity> entityValidator)
        {
            this._entityValidator = entityValidator;
        }

        protected virtual ValidationResult ValidateEntity(TEntity entity)
        {
            var result = new ValidationResult();
            if (_entityValidator != null)
            {
                result = _entityValidator.Validate(entity);
            }
            return result;
        }

        protected virtual void EvaluateBusinessRules(TEntity entity)
        {
            if (_businessRulesEvaluator != null)
            {
                _businessRulesEvaluator.Evaluate(entity);
            }
        }

        public virtual async Task<CommandResult<bool>> CreateAsync(TEntity entity)
        {
            var result = new CommandResult<bool>();
            try
            {
                result.ValidationResult = this.ValidateEntity(entity);
                if (result.ValidationResult.IsValid)
                {
                    this.EvaluateBusinessRules(entity);
                    await _repository.AddAsync(entity);
                    this.Logger.LogDebug("Creating entity of type {0}.", entity);
                    result.DataResult = true;
                }
                else
                {
                    this.Logger.LogWarning("Validator of type " + this._entityValidator.GetType().ToString() + " was not able to validate entity of type " + entity.GetType().ToString());
                    result.DataResult = false;
                }
                
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
            }
            return result;
        }

        public virtual async Task<CommandResult<bool>> UpdateAsync(TEntity entity)
        {
            var result = new CommandResult<bool>();
            try
            {
                result.ValidationResult = this.ValidateEntity(entity);
                if (result.ValidationResult.IsValid)
                {
                    this.EvaluateBusinessRules(entity);
                    await _repository.UpdateAsync(entity);
                    this.Logger.LogInformation("Updating entity of type {0}.", entity);
                    result.DataResult = true;
                }
                else
                {
                    this.Logger.LogWarning("Validator of type " + this._entityValidator.GetType().ToString() + " was not able to validate entity of type " + entity.GetType().ToString());
                    result.DataResult = false;
                }
                
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
                
            }
            return result;
        }

        public virtual async Task<CommandResult<bool>> DeleteAsync(TEntity entity)
        {
            var result = new CommandResult<bool>();
            try
            {
                result.ValidationResult = this.ValidateEntity(entity);
                if (result.ValidationResult.IsValid)
                {
                    this.EvaluateBusinessRules(entity);
                    await _repository.DeleteAsync(entity);
                    this.Logger.LogInformation("Deleting entity of type {0}.", entity);
                    result.DataResult = true;
                }
                else
                {
                    this.Logger.LogWarning("Validator of type " + this._entityValidator.GetType().ToString() + " was not able to validate entity of type " + entity.GetType().ToString());
                    result.DataResult = false;
                }
                
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
                
            }
            return result;
        }

        public virtual async Task<CommandResult<TEntity>> GetByIdAsync(object primaryKey)
        {
            var result = new CommandResult<TEntity>();
            try
            {
                if (primaryKey == null)
                {
                    result.ValidationResult.AddError(new ValidationError("Primary Key cannot be null", "primaryKey"));
                }

                if (result.ValidationResult.IsValid)
                {
                    result.DataResult = await _repository.FindAsync(primaryKey);
                    this.Logger.LogDebug("Getting entity of type {0} by Id: {1}.", typeof(TEntity), primaryKey);
                }
                else
                {
                    this.Logger.LogWarning("Input was not validated for GetByIdAsync method - primaryKey of {0}", primaryKey);
                }
                
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
                
            }

            return result;
        }

        public virtual async Task<CommandResult<ICollection<TEntity>>> GetAllAsync()
        {
            var result = new CommandResult<ICollection<TEntity>>();
            try
            {

                result.DataResult = await _repository.FindAsync(x=>true);
                this.Logger.LogDebug("Getting all entities of type {0}.", typeof(TEntity));
                
                
            }
            catch (ApplicationException ex)
            {
                result.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
                
            }
            return result;
        }
    }
}
