export function formatPrice(amount: number, currency: string): string {
  if (amount === 0) {
    return 'Free'
  }

  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount)
}
