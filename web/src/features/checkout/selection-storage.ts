import type { StartCheckoutResponse } from './api'

export type TicketSelection = Record<number, number>

const STORAGE_PREFIX = 'eventhub.checkoutSelection'

function storageKey(slug: string) {
  return `${STORAGE_PREFIX}.${slug}`
}

export function encodeTicketSelection(selection: TicketSelection) {
  return Object.entries(selection)
    .filter(([, quantity]) => quantity > 0)
    .map(([ticketTypeId, quantity]) => `${ticketTypeId}:${quantity}`)
    .join(',')
}

export function decodeTicketSelection(value: string | null): TicketSelection {
  if (!value) {
    return {}
  }

  return value.split(',').reduce<TicketSelection>((accumulator, item) => {
    const [ticketTypeIdValue, quantityValue] = item.split(':')
    const ticketTypeId = Number(ticketTypeIdValue)
    const quantity = Number(quantityValue)

    if (
      Number.isInteger(ticketTypeId) &&
      ticketTypeId > 0 &&
      Number.isInteger(quantity) &&
      quantity > 0
    ) {
      accumulator[ticketTypeId] = quantity
    }

    return accumulator
  }, {})
}

export function selectionToLines(selection: TicketSelection) {
  return Object.entries(selection)
    .map(([ticketTypeId, quantity]) => ({
      ticketTypeId: Number(ticketTypeId),
      quantity,
    }))
    .filter(
      (line) => Number.isInteger(line.ticketTypeId) && line.ticketTypeId > 0 && line.quantity > 0,
    )
}

export function readStoredSelection(slug: string): TicketSelection {
  if (typeof window === 'undefined') {
    return {}
  }

  return decodeTicketSelection(window.sessionStorage.getItem(storageKey(slug)))
}

export function writeStoredSelection(slug: string, selection: TicketSelection) {
  if (typeof window === 'undefined') {
    return
  }

  const encoded = encodeTicketSelection(selection)
  if (encoded) {
    window.sessionStorage.setItem(storageKey(slug), encoded)
  } else {
    window.sessionStorage.removeItem(storageKey(slug))
  }
}

export function writeCheckoutSnapshot(slug: string, checkout: StartCheckoutResponse) {
  if (typeof window === 'undefined') {
    return
  }

  window.sessionStorage.setItem(`${storageKey(slug)}.snapshot`, JSON.stringify(checkout))
}

export function readCheckoutSnapshot(slug: string): StartCheckoutResponse | null {
  if (typeof window === 'undefined') {
    return null
  }

  const value = window.sessionStorage.getItem(`${storageKey(slug)}.snapshot`)
  if (!value) {
    return null
  }

  try {
    return JSON.parse(value) as StartCheckoutResponse
  } catch {
    return null
  }
}
