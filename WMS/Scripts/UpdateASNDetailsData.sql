-- Script to update existing ASNDetails data
-- This script initializes RemainingQuantity and AlreadyPutAwayQuantity for existing records

-- Update RemainingQuantity = ShippedQuantity for all ASNDetails where RemainingQuantity is 0
UPDATE ASNDetails 
SET RemainingQuantity = ShippedQuantity,
    AlreadyPutAwayQuantity = 0
WHERE RemainingQuantity = 0 AND AlreadyPutAwayQuantity = 0;

-- Update AlreadyPutAwayQuantity based on existing inventory records
-- This calculates how much has already been put away by checking inventory records
UPDATE ad 
SET AlreadyPutAwayQuantity = ISNULL((
    SELECT SUM(i.Quantity) 
    FROM Inventories i 
    WHERE i.ItemId = ad.ItemId 
    AND i.CompanyId = ad.CompanyId 
    AND i.SourceReference LIKE 'ASN-' + CAST(ad.ASNId AS VARCHAR) + '-' + CAST(ad.Id AS VARCHAR) + '%'
), 0),
RemainingQuantity = ad.ShippedQuantity - ISNULL((
    SELECT SUM(i.Quantity) 
    FROM Inventories i 
    WHERE i.ItemId = ad.ItemId 
    AND i.CompanyId = ad.CompanyId 
    AND i.SourceReference LIKE 'ASN-' + CAST(ad.ASNId AS VARCHAR) + '-' + CAST(ad.Id AS VARCHAR) + '%'
), 0)
FROM ASNDetails ad
WHERE ad.ShippedQuantity > 0;

-- Verify the update
SELECT 
    Id,
    ASNId,
    ItemId,
    ShippedQuantity,
    AlreadyPutAwayQuantity,
    RemainingQuantity,
    (ShippedQuantity - AlreadyPutAwayQuantity) as CalculatedRemaining
FROM ASNDetails 
WHERE ShippedQuantity > 0
ORDER BY ASNId, Id;
