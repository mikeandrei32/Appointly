export default function TimeSlotPicker({ slots, selected, onChange, disabled }) {
  return <fieldset disabled={disabled}>
    <legend className="mb-3 text-sm font-semibold">Available times · Manila time</legend>
    <div className="grid grid-cols-3 gap-3 sm:grid-cols-4">
      {slots.map((slot) => <label key={slot.startUtc} className={`cursor-pointer rounded-xl border p-3 text-center text-sm font-medium ${selected === slot.startUtc ? 'border-blue-600 bg-blue-50 text-blue-800' : 'border-slate-200 hover:border-blue-400'}`}>
        <input type="radio" name="slot" value={slot.startUtc} checked={selected === slot.startUtc}
          onChange={() => onChange(slot.startUtc)} className="mr-2 accent-blue-600" />
        {slot.localTime}
      </label>)}
    </div>
  </fieldset>
}
