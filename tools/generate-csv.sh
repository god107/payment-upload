#!/bin/bash
# Generate a CSV file with random payment instructions
# Usage: ./generate-csv.sh <number_of_rows> [output_file]
#
# Examples:
#   ./generate-csv.sh 1000                    # Creates test-payments-1000.csv
#   ./generate-csv.sh 100000 large-test.csv   # Creates large-test.csv with 100k rows

set -e

# Check arguments
if [ -z "$1" ]; then
    echo "Usage: $0 <number_of_rows> [output_file]"
    echo "Example: $0 10000 test-payments.csv"
    exit 1
fi

NUM_ROWS=$1
OUTPUT_FILE="${2:-test-payments-${NUM_ROWS}.csv}"

# Arrays for random data generation
FIRST_NAMES=("John" "Jane" "Michael" "Sarah" "David" "Emma" "James" "Emily" "Robert" "Olivia" "William" "Sophia" "Richard" "Isabella" "Thomas" "Mia" "Charles" "Charlotte" "Daniel" "Amelia")
LAST_NAMES=("Smith" "Johnson" "Williams" "Brown" "Jones" "Garcia" "Miller" "Davis" "Rodriguez" "Martinez" "Wilson" "Anderson" "Taylor" "Thomas" "Moore" "Jackson" "Martin" "Lee" "Thompson" "White")
CITIES=("London" "Manchester" "Birmingham" "Leeds" "Glasgow" "Liverpool" "Bristol" "Sheffield" "Edinburgh" "Cardiff")
BANKS=("Barclays" "HSBC" "Lloyds" "NatWest" "Santander" "Halifax" "Nationwide" "TSB" "Metro Bank" "Monzo")
SWIFT_CODES=("BABORB2L" "HBUKGB4B" "LOYDGB2L" "NWBKGB2L" "ABBYGB2L" "HLFXGB21" "NAIAGB21" "TSBSGB2A" "MYMBGB2L" "MONZGB2L")
CURRENCIES=("GBP" "EUR" "USD")
COUNTRIES=("GB" "DE" "FR" "US" "NL" "BE" "IT" "ES")
ARRIVE_BY=("ASAP" "STANDARD" "NEXT_DAY")

# Function to generate random string of digits
random_digits() {
    local length=$1
    LC_CTYPE=C tr -dc '0-9' </dev/urandom | head -c "$length"
}

# Function to generate random IBAN
generate_iban() {
    local country=${COUNTRIES[$((RANDOM % ${#COUNTRIES[@]}))]}
    local check=$(random_digits 2)
    local bban=$(random_digits 18)
    echo "${country}${check}${bban}"
}

# Function to generate random date in DD.MM.YYYY format
generate_date() {
    local day=$((RANDOM % 28 + 1))
    local month=$((RANDOM % 12 + 1))
    local year=$((RANDOM % 5 + 2020))
    printf "%02d.%02d.%d" "$day" "$month" "$year"
}

# Function to generate random amount
generate_amount() {
    local whole=$((RANDOM % 10000 + 1))
    local decimal=$((RANDOM % 100))
    printf "%d.%02d" "$whole" "$decimal"
}

echo "Generating $NUM_ROWS payment instructions to $OUTPUT_FILE..."

# Write header
echo "BeneficiaryName,UniqueReference,AccountHolder,Address1,Address2,City,County,Postcode,Country,PaymentCurrency,BankName,BankCountry,AccountNumber,SwiftCode,IbanNumber,OtherRouting,BranchCode,AccountType,Telephone,Email,SettlementCurrency,Reference,Amount,ArriveBy,TransactionCode,OwningSide,Method,CountryCode,InvoiceNumber,InvoiceDate" > "$OUTPUT_FILE"

# Progress indicator
PROGRESS_INTERVAL=$((NUM_ROWS / 10))
if [ "$PROGRESS_INTERVAL" -eq 0 ]; then
    PROGRESS_INTERVAL=1
fi

# Generate rows
for ((i=1; i<=NUM_ROWS; i++)); do
    # Random selections
    FIRST_NAME=${FIRST_NAMES[$((RANDOM % ${#FIRST_NAMES[@]}))]}
    LAST_NAME=${LAST_NAMES[$((RANDOM % ${#LAST_NAMES[@]}))]}
    FULL_NAME="$FIRST_NAME $LAST_NAME"
    
    CITY=${CITIES[$((RANDOM % ${#CITIES[@]}))]}
    BANK=${BANKS[$((RANDOM % ${#BANKS[@]}))]}
    SWIFT=${SWIFT_CODES[$((RANDOM % ${#SWIFT_CODES[@]}))]}
    CURRENCY=${CURRENCIES[$((RANDOM % ${#CURRENCIES[@]}))]}
    COUNTRY=${COUNTRIES[$((RANDOM % ${#COUNTRIES[@]}))]}
    ARRIVE=${ARRIVE_BY[$((RANDOM % ${#ARRIVE_BY[@]}))]}
    
    IBAN=$(generate_iban)
    AMOUNT=$(generate_amount)
    DATE=$(generate_date)
    INVOICE_NUM=$((10000000 + i))
    ADDRESS_NUM=$((RANDOM % 200 + 1))
    POSTCODE="${COUNTRY}$(random_digits 2) $(random_digits 1)${FIRST_NAME:0:2}"
    
    # Build CSV row
    # BeneficiaryName,UniqueReference,AccountHolder,Address1,Address2,City,County,Postcode,Country,
    # PaymentCurrency,BankName,BankCountry,AccountNumber,SwiftCode,IbanNumber,OtherRouting,BranchCode,
    # AccountType,Telephone,Email,SettlementCurrency,Reference,Amount,ArriveBy,TransactionCode,
    # OwningSide,Method,CountryCode,InvoiceNumber,InvoiceDate
    
    echo "${FULL_NAME},${IBAN},${FULL_NAME},${ADDRESS_NUM} High Street,,${CITY},,,${COUNTRY},${CURRENCY},${BANK},${COUNTRY},,${SWIFT},${IBAN},,,,,,USD,Invoice ${INVOICE_NUM},${AMOUNT},${ARRIVE},,SELL,,,,${DATE}" >> "$OUTPUT_FILE"
    
    # Progress indicator
    if [ $((i % PROGRESS_INTERVAL)) -eq 0 ]; then
        PERCENT=$((i * 100 / NUM_ROWS))
        echo "  Progress: ${PERCENT}% ($i/$NUM_ROWS rows)"
    fi
done

# File stats
FILE_SIZE=$(du -h "$OUTPUT_FILE" | cut -f1)
echo ""
echo "✓ Generated $OUTPUT_FILE"
echo "  - Rows: $NUM_ROWS"
echo "  - Size: $FILE_SIZE"
echo ""
echo "To upload: curl -F \"file=@${OUTPUT_FILE}\" http://localhost:5099/api/payment-uploads"
