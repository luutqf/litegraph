export class TransactionOperationResult {
  constructor(result = {}) {
    const {
      Index = 0,
      OperationType = 'Create',
      ObjectType = 'Node',
      GUID = null,
      Success = true,
      Result = null,
      Error = null,
    } = result;

    this.Index = Index;
    this.OperationType = OperationType;
    this.ObjectType = ObjectType;
    this.GUID = GUID;
    this.Success = Success;
    this.Result = Result;
    this.Error = Error;
  }
}

export default class TransactionResult {
  constructor(result = {}) {
    const {
      Success = true,
      RolledBack = false,
      FailedOperationIndex = null,
      Error = null,
      Operations = [],
      DurationMs = 0,
    } = result;

    this.Success = Success;
    this.RolledBack = RolledBack;
    this.FailedOperationIndex = FailedOperationIndex;
    this.Error = Error;
    this.Operations = Operations.map((operation) => new TransactionOperationResult(operation));
    this.DurationMs = DurationMs;
  }
}
