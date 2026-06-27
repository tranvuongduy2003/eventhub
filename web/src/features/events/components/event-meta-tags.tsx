import { Helmet } from 'react-helmet-async'
import { buildAllMetaTags, type OgTagData } from '@/lib/og-tags'

type EventMetaTagsProps = {
  event: OgTagData
}

export function EventMetaTags({ event }: EventMetaTagsProps) {
  const baseUrl = window.location.origin
  const tags = buildAllMetaTags(event, baseUrl)

  return (
    <Helmet>
      {tags.map((tag) =>
        tag.property ? (
          <meta key={tag.property} property={tag.property} content={tag.content} />
        ) : (
          <meta key={tag.name} name={tag.name} content={tag.content} />
        ),
      )}
    </Helmet>
  )
}
