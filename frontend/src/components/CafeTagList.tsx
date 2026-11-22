import { CAFE_TAGS, knownTags } from '../lib/cafeTags';

type CafeTagListProps = {
  tags: readonly string[] | undefined;
};

/** Servicios de la cafetería (wifi, terraza, enchufes…) como etiquetas. */
export function CafeTagList({ tags }: CafeTagListProps) {
  const items = knownTags(tags);

  if (items.length === 0) {
    return (
      <p className="mt-3 text-xs text-coffee-500">
        Sin información de servicios (wifi, terraza, enchufes…).
      </p>
    );
  }

  return (
    <ul className="mt-3 flex flex-wrap gap-1.5" aria-label="Servicios">
      {items.map((tag) => {
        const { label, icon: Icon } = CAFE_TAGS[tag];

        return (
          <li
            key={tag}
            className="inline-flex items-center gap-1 rounded-full border border-coffee-100 bg-cream-50 px-2.5 py-1 text-[11px] font-medium text-coffee-700"
          >
            <Icon className="h-3.5 w-3.5 text-caramel-600" aria-hidden="true" />
            {label}
          </li>
        );
      })}
    </ul>
  );
}
