const SITE_NAME = 'EventHub'
const MAX_DESCRIPTION_LENGTH = 200

export type OgTagData = {
  title: string
  description: string | null
  startsAt: string | null
  endsAt: string | null
  physicalAddress: string | null
  isOnline: boolean
  coverImageUrl: string | null
  slug: string
}

export type MetaTag = {
  name?: string
  property?: string
  content: string
}

function formatDateLocation(
  startsAt: string | null,
  physicalAddress: string | null,
  isOnline: boolean,
): string {
  const parts: string[] = []

  if (startsAt) {
    const date = new Date(startsAt)
    const dateStr = date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    })
    const timeStr = date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
    })
    parts.push(`${dateStr}, ${timeStr}`)
  }

  if (physicalAddress) {
    parts.push(physicalAddress)
  } else if (isOnline) {
    parts.push('Online event')
  }

  return parts.join(' · ') || 'Event on EventHub'
}

function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text
  return text.slice(0, maxLength - 1).trimEnd() + '…'
}

export function buildOgTags(data: OgTagData, baseUrl: string): MetaTag[] {
  const eventUrl = `${baseUrl}/events/${data.slug}`
  const description = formatDateLocation(data.startsAt, data.physicalAddress, data.isOnline)
  const truncatedDescription = truncate(description, MAX_DESCRIPTION_LENGTH)

  const tags: MetaTag[] = [
    { property: 'og:title', content: data.title },
    { property: 'og:description', content: truncatedDescription },
    { property: 'og:type', content: 'event' },
    { property: 'og:url', content: eventUrl },
    { property: 'og:site_name', content: SITE_NAME },
  ]

  if (data.coverImageUrl) {
    tags.push({ property: 'og:image', content: data.coverImageUrl })
    tags.push({
      property: 'og:image:alt',
      content: `Cover image for ${data.title}`,
    })
  }

  return tags
}

export function buildTwitterTags(data: OgTagData): MetaTag[] {
  const description = formatDateLocation(data.startsAt, data.physicalAddress, data.isOnline)
  const truncatedDescription = truncate(description, MAX_DESCRIPTION_LENGTH)

  const tags: MetaTag[] = [
    {
      name: 'twitter:card',
      content: data.coverImageUrl ? 'summary_large_image' : 'summary',
    },
    { name: 'twitter:title', content: data.title },
    { name: 'twitter:description', content: truncatedDescription },
  ]

  if (data.coverImageUrl) {
    tags.push({ name: 'twitter:image', content: data.coverImageUrl })
  }

  return tags
}

export function buildAllMetaTags(data: OgTagData, baseUrl: string): MetaTag[] {
  return [...buildOgTags(data, baseUrl), ...buildTwitterTags(data)]
}
