export default class TransactionOperation {
  constructor(operation = {}) {
    const { OperationType = 'Create', ObjectType = 'Node', GUID = null, Payload = null } = operation;

    this.OperationType = OperationType;
    this.ObjectType = ObjectType;
    this.GUID = GUID;
    this.Payload = Payload;
  }
}
