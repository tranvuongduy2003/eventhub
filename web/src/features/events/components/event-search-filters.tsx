import { useQuery } from '@tanstack/react-query'
import { FilterIcon, SearchIcon, XIcon } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { CollapsibleRoot, CollapsibleTrigger, CollapsiblePanel } from '@/components/ui/collapsible'
import { getEventLocations } from '@/features/events/api'

type EventSearchFiltersProps = {
  searchValue: string
  onSearchChange: (value: string) => void
  dateValue: string
  onDateChange: (value: string) => void
  locationValue: string
  onLocationChange: (value: string) => void
  onClear: () => void
  hasActiveFilters: boolean
}

const DATE_OPTIONS = [
  { value: '', label: 'Any date' },
  { value: 'today', label: 'Today' },
  { value: 'tomorrow', label: 'Tomorrow' },
  { value: 'this-week', label: 'This week' },
  { value: 'this-month', label: 'This month' },
]

export function EventSearchFilters({
  searchValue,
  onSearchChange,
  dateValue,
  onDateChange,
  locationValue,
  onLocationChange,
  onClear,
  hasActiveFilters,
}: EventSearchFiltersProps) {
  const { data: locations } = useQuery({
    queryKey: ['events', 'locations'],
    queryFn: ({ signal }) => getEventLocations(signal),
    staleTime: 5 * 60_000,
  })

  return (
    <div className="flex flex-col gap-3">
      {/* Search input — always visible */}
      <div className="relative">
        <SearchIcon className="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
        <Input
          type="search"
          placeholder="Search events..."
          value={searchValue}
          onChange={(event) => onSearchChange(event.target.value)}
          className="h-10 pl-9"
        />
      </div>

      {/* Mobile: collapsible filters */}
      <CollapsibleRoot defaultOpen={false} className="md:hidden">
        <div className="flex items-center justify-between">
          <CollapsibleTrigger className="text-muted-foreground hover:text-foreground inline-flex min-h-[44px] items-center gap-1.5 text-sm font-medium">
            <FilterIcon className="size-4" />
            Filters
          </CollapsibleTrigger>
          {hasActiveFilters && (
            <Button variant="ghost" size="sm" onClick={onClear} className="min-h-[44px]">
              <XIcon className="size-4" />
              Clear
            </Button>
          )}
        </div>
        <CollapsiblePanel className="flex flex-col gap-3 pt-3">
          <DateSelect value={dateValue} onValueChange={onDateChange} />
          <LocationSelect
            value={locationValue}
            onValueChange={onLocationChange}
            locations={locations}
          />
        </CollapsiblePanel>
      </CollapsibleRoot>

      {/* Desktop: inline filters */}
      <div className="hidden items-center gap-3 md:flex">
        <DateSelect value={dateValue} onValueChange={onDateChange} />
        <LocationSelect
          value={locationValue}
          onValueChange={onLocationChange}
          locations={locations}
        />
        {hasActiveFilters && (
          <Button variant="ghost" size="sm" onClick={onClear} className="min-h-[44px]">
            <XIcon className="size-4" />
            Clear filters
          </Button>
        )}
      </div>
    </div>
  )
}

function DateSelect({
  value,
  onValueChange,
}: {
  value: string
  onValueChange: (value: string) => void
}) {
  return SelectWrapper({ value, onValueChange, options: DATE_OPTIONS, placeholder: 'Any date' })
}

function LocationSelect({
  value,
  onValueChange,
  locations,
}: {
  value: string
  onValueChange: (value: string) => void
  locations: string[] | undefined
}) {
  const options = [
    { value: '', label: 'Any location' },
    ...(locations?.map((loc) => ({ value: loc, label: loc })) ?? []),
  ]

  return SelectWrapper({ value, onValueChange, options, placeholder: 'Any location' })
}

function SelectWrapper({
  value,
  onValueChange,
  options,
  placeholder,
}: {
  value: string
  onValueChange: (value: string) => void
  options: { value: string; label: string }[]
  placeholder: string
}) {
  return (
    <Select value={value} onValueChange={(val) => onValueChange(val ?? '')}>
      <SelectTrigger className="w-full min-w-[140px] md:w-auto">
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
