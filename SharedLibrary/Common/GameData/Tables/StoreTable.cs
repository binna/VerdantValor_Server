using Common.Error;
using Shared.GameData;

namespace Common.GameData.Tables;

public class StoreTable : BaseTable<Store>
{
    public StoreTable(string tableName) : base(tableName)
    { }

    public override bool Load(string path)
    {
        var data = LoadFromJson<Store>(path, TableName);
        
        mTable.Clear();
        
        foreach (var store in data.Data)
        {
            mTable[store.Id] = store;
        }

        return true;
    }

    public override void Validate()
    {
        // 별도의 검증 로직이 필요 없음
    }

    public override void CrossValidate(ITableRegistry registry)
    {
        var errors = new List<ValidationError>();
        var itemTable = registry.GetTable<ItemTable>();
        
        if (itemTable == null)
            throw new InvalidOperationException(
                string.Format(ErrorMessages.TABLE_NOT_FOUND, "Item"));
        
        foreach (var store in mTable.Values)
        {
            foreach (var item in store.Items)
            {
                if (!itemTable.TryGet(item.Id, out _))
                {
                    errors.Add(new ValidationError(
                        context: $"StoreId={store.Id},ItemId={item.Id}",
                        field: nameof(item.Id),
                        type: ValidationError.ValidationErrorType.NotFound,
                        message: "Item not found in ItemTable"));
                    continue;
                }

                if (item.Amount <= 0)
                {
                    errors.Add(new ValidationError(
                        context: $"StoreId={store.Id},ItemId={item.Id}",
                        field: nameof(item.Amount),
                        type: ValidationError.ValidationErrorType.InvalidValue,
                        message: $"Invalid amount: {item.Amount}. Amount must be greater than 0."));
                }
            }
            
            foreach (var price in store.Prices)
            {
                if (price.Value <= 0)
                {
                    errors.Add(new ValidationError(
                        context: $"StoreId={store.Id},Currency={price.Currency}",
                        field: nameof(price.Value),
                        type: ValidationError.ValidationErrorType.InvalidValue,
                        message: $"Invalid price: {price.Value}. Price must be greater than 0."));
                }
            }
        }

        if (errors.Count > 0)
        {
            var errorText = string.Join(
                Environment.NewLine,
                errors.Select(x => x.ToString()));

            throw new InvalidOperationException(
                $"StoreTable Cross validation failed.{Environment.NewLine}{errorText}");
        }
    }
}